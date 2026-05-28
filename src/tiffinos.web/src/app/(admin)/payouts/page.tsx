"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { PayoutRecord } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
	Dialog,
	DialogContent,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { Wallet, RefreshCw, CheckCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

const settleSchema = z.object({
	paymentMethod: z.string().min(1, "Payment method required"),
	paymentReference: z.string().optional(),
});

type SettleForm = z.infer<typeof settleSchema>;

const statusConfig: Record<string, { label: string; className: string }> = {
	pending: { label: "Pending", className: "bg-amber-100 text-amber-700" },
	approved: { label: "Approved", className: "bg-blue-100 text-blue-700" },
	paid: { label: "Paid", className: "bg-green-100 text-green-700" },
};

export default function PayoutsPage() {
	const queryClient = useQueryClient();
	const [selected, setSelected] = useState<Set<string>>(new Set());
	const [settleDialogOpen, setSettleDialog] = useState(false);
	const [activeTab, setActiveTab] = useState<"records" | "settlements">(
		"records",
	);

	// ── Queries ───────────────────────────────────────────────
	const { data: payoutsData, isLoading } = useQuery<{
		records: PayoutRecord[];
		summary: {
			totalRecords: number;
			totalAmount: number;
			pendingAmount: number;
			approvedAmount: number;
			paidAmount: number;
		};
	}>({
		queryKey: ["payouts"],
		queryFn: () => api.get("/api/payouts").then((r) => r.data),
	});

	const { data: settlements = [] } = useQuery<any[]>({
		queryKey: ["settlements"],
		queryFn: () => api.get("/api/payouts/settlements").then((r) => r.data),
	});

	// ── Mutations ─────────────────────────────────────────────
	const generatePayouts = useMutation({
		mutationFn: () => api.post("/api/payouts/generate-today"),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["payouts"] });
			toast.success("Payouts generated for today");
		},
		onError: () => toast.error("Failed to generate payouts"),
	});

	const approvePayout = useMutation({
		mutationFn: (id: string) =>
			api.post(`/api/payouts/${id}/approve`, { notes: null }),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["payouts"] });
			toast.success("Payout approved");
		},
		onError: () => toast.error("Failed to approve payout"),
	});

	const settlePayouts = useMutation({
		mutationFn: (data: SettleForm) =>
			api.post("/api/payouts/settle", {
				payoutRecordIds: Array.from(selected),
				paymentMethod: data.paymentMethod,
				paymentReference: data.paymentReference || null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["payouts"] });
			queryClient.invalidateQueries({ queryKey: ["settlements"] });
			setSettleDialog(false);
			setSelected(new Set());
			toast.success("Payouts settled successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to settle payouts"),
	});

	const records = payoutsData?.records ?? [];
	const summary = payoutsData?.summary;
	const approved = records.filter((r) => r.status === "approved");
	const allSelected =
		approved.length > 0 && approved.every((r) => selected.has(r.id));

	const toggleSelect = (id: string) => {
		setSelected((prev) => {
			const next = new Set(prev);
			next.has(id) ? next.delete(id) : next.add(id);
			return next;
		});
	};

	const toggleAll = () => {
		if (allSelected) {
			setSelected(new Set());
		} else {
			setSelected(new Set(approved.map((r) => r.id)));
		}
	};

	const form = useForm<SettleForm>({
		resolver: zodResolver(settleSchema),
		defaultValues: { paymentMethod: "", paymentReference: "" },
	});

	return (
		<div className="space-y-6">
			{/* Summary Cards */}
			{summary && (
				<div className="grid grid-cols-4 gap-4">
					{[
						{
							label: "Total",
							amount: summary.totalAmount,
							color: "text-slate-800",
						},
						{
							label: "Pending",
							amount: summary.pendingAmount,
							color: "text-amber-600",
						},
						{
							label: "Approved",
							amount: summary.approvedAmount,
							color: "text-blue-600",
						},
						{
							label: "Paid",
							amount: summary.paidAmount,
							color: "text-green-600",
						},
					].map((item) => (
						<div
							key={item.label}
							className="bg-white rounded-xl border border-slate-200 p-4">
							<p className="text-xs text-slate-500">{item.label}</p>
							<p className={cn("text-2xl font-bold mt-1", item.color)}>
								${item.amount.toFixed(2)}
							</p>
						</div>
					))}
				</div>
			)}

			{/* Tabs + Actions */}
			<div className="flex items-center justify-between">
				<div className="flex gap-2">
					<Button
						variant={activeTab === "records" ? "default" : "outline"}
						size="sm"
						onClick={() => setActiveTab("records")}>
						Payout Records ({records.length})
					</Button>
					<Button
						variant={activeTab === "settlements" ? "default" : "outline"}
						size="sm"
						onClick={() => setActiveTab("settlements")}>
						Settlements ({settlements.length})
					</Button>
				</div>
				<div className="flex gap-2">
					{selected.size > 0 && (
						<Button
							size="sm"
							className="bg-green-600 hover:bg-green-700"
							onClick={() => setSettleDialog(true)}>
							<CheckCircle size={14} className="mr-2" />
							Settle {selected.size} Record{selected.size > 1 ? "s" : ""}
						</Button>
					)}
					<Button
						variant="outline"
						size="sm"
						onClick={() => generatePayouts.mutate()}
						disabled={generatePayouts.isPending}>
						<RefreshCw size={14} className="mr-2" />
						Generate Today
					</Button>
				</div>
			</div>

			{/* Payout Records Tab */}
			{activeTab === "records" &&
				(isLoading ? (
					<div className="space-y-3">
						{Array.from({ length: 3 }).map((_, i) => (
							<Skeleton key={i} className="h-16 w-full rounded-xl" />
						))}
					</div>
				) : records.length === 0 ? (
					<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
						<Wallet size={32} className="text-slate-300 mx-auto mb-3" />
						<p className="text-slate-400 text-sm">No payout records yet.</p>
						<Button
							size="sm"
							className="mt-4 bg-blue-600 hover:bg-blue-700"
							onClick={() => generatePayouts.mutate()}>
							Generate Today's Payouts
						</Button>
					</div>
				) : (
					<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
						<table className="w-full">
							<thead>
								<tr className="border-b border-slate-100 bg-slate-50">
									<th className="px-5 py-3 w-8">
										<input
											type="checkbox"
											checked={allSelected}
											onChange={toggleAll}
											className="rounded"
										/>
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Driver
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Date
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Type
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Deliveries
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Base
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Bonus
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Total
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Status
									</th>
									<th className="px-5 py-3" />
								</tr>
							</thead>
							<tbody>
								{records.map((record) => {
									const status = statusConfig[record.status] ?? {
										label: record.status,
										className: "bg-slate-100 text-slate-500",
									};
									const isApproved = record.status === "approved";
									return (
										<tr
											key={record.id}
											className={cn(
												"border-b border-slate-50 last:border-0 hover:bg-slate-50",
												selected.has(record.id) && "bg-blue-50",
											)}>
											<td className="px-5 py-3">
												{isApproved && (
													<input
														type="checkbox"
														checked={selected.has(record.id)}
														onChange={() => toggleSelect(record.id)}
														className="rounded"
													/>
												)}
											</td>
											<td className="px-5 py-3">
												<p className="text-sm font-medium text-slate-800">
													{record.driverName}
												</p>
												<p className="text-xs text-slate-400">
													{record.policyName}
												</p>
											</td>
											<td className="px-5 py-3 text-sm text-slate-600">
												{record.payoutDate}
											</td>
											<td className="px-5 py-3">
												<code className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">
													{record.payoutType}
												</code>
											</td>
											<td className="px-5 py-3 text-sm text-slate-600">
												{record.totalDeliveries}
											</td>
											<td className="px-5 py-3 text-sm text-slate-600">
												${record.baseAmount.toFixed(2)}
											</td>
											<td className="px-5 py-3 text-sm text-slate-600">
												${record.bonusAmount.toFixed(2)}
											</td>
											<td className="px-5 py-3 text-sm font-semibold text-slate-800">
												${record.totalAmount.toFixed(2)}
											</td>
											<td className="px-5 py-3">
												<Badge
													variant="secondary"
													className={cn("text-xs", status.className)}>
													{status.label}
												</Badge>
											</td>
											<td className="px-5 py-3 text-right">
												{record.status === "pending" && (
													<Button
														size="sm"
														variant="outline"
														className="h-7 text-xs"
														onClick={() => approvePayout.mutate(record.id)}>
														Approve
													</Button>
												)}
											</td>
										</tr>
									);
								})}
							</tbody>
						</table>
					</div>
				))}

			{/* Settlements Tab */}
			{activeTab === "settlements" && (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					{settlements.length === 0 ? (
						<div className="p-12 text-center">
							<p className="text-slate-400 text-sm">No settlements yet.</p>
						</div>
					) : (
						<table className="w-full">
							<thead>
								<tr className="border-b border-slate-100 bg-slate-50">
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Driver
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Period
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Amount
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Method
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Reference
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Processed
									</th>
								</tr>
							</thead>
							<tbody>
								{settlements.map((s: any) => (
									<tr
										key={s.id}
										className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
										<td className="px-5 py-3 text-sm font-medium text-slate-800">
											{s.driverName}
										</td>
										<td className="px-5 py-3 text-sm text-slate-600">
											{s.periodStart} → {s.periodEnd}
										</td>
										<td className="px-5 py-3 text-sm font-semibold text-slate-800">
											${s.totalAmount.toFixed(2)}
										</td>
										<td className="px-5 py-3">
											<code className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">
												{s.paymentMethod}
											</code>
										</td>
										<td className="px-5 py-3 text-xs text-slate-500">
											{s.paymentReference ?? "—"}
										</td>
										<td className="px-5 py-3 text-xs text-slate-500">
											{s.processedAt
												? new Date(s.processedAt).toLocaleDateString()
												: "—"}
										</td>
									</tr>
								))}
							</tbody>
						</table>
					)}
				</div>
			)}

			{/* Settle Dialog */}
			<Dialog open={settleDialogOpen} onOpenChange={setSettleDialog}>
				<DialogContent className="sm:max-w-md">
					<DialogHeader>
						<DialogTitle>
							Settle {selected.size} Payout Record{selected.size > 1 ? "s" : ""}
						</DialogTitle>
					</DialogHeader>
					<form
						onSubmit={form.handleSubmit((data) => settlePayouts.mutate(data))}
						className="space-y-4 pt-2">
						<div className="bg-slate-50 rounded-lg p-3">
							<p className="text-xs text-slate-500">Total amount to settle</p>
							<p className="text-2xl font-bold text-slate-800 mt-1">
								$
								{records
									.filter((r) => selected.has(r.id))
									.reduce((sum, r) => sum + r.totalAmount, 0)
									.toFixed(2)}{" "}
								CAD
							</p>
						</div>

						<div className="space-y-2">
							<Label>Payment Method *</Label>
							<Select
								value={form.watch("paymentMethod")}
								onValueChange={(v) => form.setValue("paymentMethod", v)}>
								<SelectTrigger>
									<SelectValue placeholder="Select method" />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value="cash">Cash</SelectItem>
									<SelectItem value="bank_transfer">Bank Transfer</SelectItem>
									<SelectItem value="upi">UPI / E-Transfer</SelectItem>
								</SelectContent>
							</Select>
							{form.formState.errors.paymentMethod && (
								<p className="text-xs text-red-500">
									{form.formState.errors.paymentMethod.message}
								</p>
							)}
						</div>

						<div className="space-y-2">
							<Label>Payment Reference</Label>
							<Input
								placeholder="Transaction ID, reference number"
								{...form.register("paymentReference")}
							/>
						</div>

						<div className="flex justify-end gap-2 pt-2">
							<Button
								type="button"
								variant="outline"
								onClick={() => setSettleDialog(false)}>
								Cancel
							</Button>
							<Button
								type="submit"
								className="bg-green-600 hover:bg-green-700"
								disabled={settlePayouts.isPending}>
								{settlePayouts.isPending ? "Settling..." : "Confirm Settlement"}
							</Button>
						</div>
					</form>
				</DialogContent>
			</Dialog>
		</div>
	);
}
