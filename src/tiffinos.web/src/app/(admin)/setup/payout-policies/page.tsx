"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import api from "@/lib/api";
import { PayoutPolicy } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
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
import { Plus, Wallet } from "lucide-react";
import { cn } from "@/lib/utils";

const schema = z.object({
	name: z.string().min(1, "Name is required"),
	payoutType: z.string().min(1, "Payout type is required"),
	baseRate: z.coerce.number().min(0),
	bonusPerDelivery: z.coerce.number().optional(),
	bonusThreshold: z.coerce.number().optional(),
	minGuaranteed: z.coerce.number().optional(),
	currency: z.string().default("CAD"),
});

type PolicyForm = z.infer<typeof schema>;

const payoutTypeLabels: Record<string, string> = {
	per_delivery: "Per Delivery — paid per completed delivery",
	per_day: "Per Day — flat daily rate",
	per_km: "Per Kilometre — paid per km driven",
	per_zone: "Per Zone — paid per zone completed",
	hybrid: "Hybrid — base rate + bonus above threshold",
};

export default function PayoutPoliciesPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);
	const [editing, setEditing] = useState<PayoutPolicy | null>(null);

	const { data: policies = [], isLoading } = useQuery<PayoutPolicy[]>({
		queryKey: ["payoutPolicies"],
		queryFn: () => api.get("/api/drivers/payout-policies").then((r) => r.data),
	});

	const form = useForm<PolicyForm>({
		resolver: zodResolver(schema),
		defaultValues: {
			name: "",
			payoutType: "",
			baseRate: 0,
			currency: "CAD",
		},
	});

	const isHybrid = form.watch("payoutType") === "hybrid";

	const createPolicy = useMutation({
		mutationFn: (data: PolicyForm) =>
			api.post("/api/drivers/payout-policies", data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["payoutPolicies"] });
			setDialogOpen(false);
			form.reset();
			toast.success("Payout policy created");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create policy"),
	});

	const openCreate = () => {
		setEditing(null);
		form.reset({
			name: "",
			payoutType: "",
			baseRate: 0,
			currency: "CAD",
		});
		setDialogOpen(true);
	};

	return (
		<div className="space-y-6 max-w-4xl">
			{/* Explanation */}
			<div className="bg-blue-50 border border-blue-100 rounded-xl p-4">
				<p className="text-sm text-blue-800 font-medium">
					What are payout policies?
				</p>
				<p className="text-xs text-blue-600 mt-1">
					Define how drivers are paid — per delivery, per day, per km, or a
					hybrid model. Create policies here then assign them to drivers in the
					Drivers page.
				</p>
			</div>

			{/* Actions */}
			<div className="flex justify-end">
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={openCreate}>
					<Plus size={16} className="mr-2" />
					Add Policy
				</Button>
			</div>

			{/* Policies List */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-16 w-full rounded-xl" />
					))}
				</div>
			) : policies.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<Wallet size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">No payout policies yet.</p>
					<p className="text-slate-400 text-xs mt-1">
						Create at least one before adding drivers.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={openCreate}>
						<Plus size={16} className="mr-2" />
						Add First Policy
					</Button>
				</div>
			) : (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					<table className="w-full">
						<thead>
							<tr className="border-b border-slate-100 bg-slate-50">
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Policy Name
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Type
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Base Rate
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Bonus
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Min Guaranteed
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Drivers
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Status
								</th>
							</tr>
						</thead>
						<tbody>
							{policies.map((policy) => (
								<tr
									key={policy.id}
									className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
									<td className="px-5 py-4">
										<p className="text-sm font-medium text-slate-800">
											{policy.name}
										</p>
									</td>
									<td className="px-5 py-4">
										<code className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">
											{policy.payoutType}
										</code>
									</td>
									<td className="px-5 py-4 text-sm text-slate-700">
										${policy.baseRate} {policy.currency}
									</td>
									<td className="px-5 py-4 text-sm text-slate-600">
										{policy.bonusPerDelivery
											? `$${policy.bonusPerDelivery} above ${policy.bonusThreshold} deliveries`
											: "—"}
									</td>
									<td className="px-5 py-4 text-sm text-slate-600">
										{policy.minGuaranteed ? `$${policy.minGuaranteed}` : "—"}
									</td>
									<td className="px-5 py-4 text-sm text-slate-600">
										{policy.driverCount}
									</td>
									<td className="px-5 py-4">
										<Badge
											variant="secondary"
											className={cn(
												"text-xs",
												policy.isActive
													? "bg-green-100 text-green-700"
													: "bg-slate-100 text-slate-500",
											)}>
											{policy.isActive ? "Active" : "Inactive"}
										</Badge>
									</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>
			)}

			{/* Dialog */}
			<Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
				<DialogContent className="sm:max-w-md">
					<DialogHeader>
						<DialogTitle>Add Payout Policy</DialogTitle>
					</DialogHeader>
					<form
						onSubmit={form.handleSubmit((d) => createPolicy.mutate(d))}
						className="space-y-4 pt-2">
						<div className="space-y-2">
							<Label>Policy Name *</Label>
							<Input
								placeholder="e.g. Standard Driver Rate"
								{...form.register("name")}
							/>
							{form.formState.errors.name && (
								<p className="text-xs text-red-500">
									{form.formState.errors.name.message}
								</p>
							)}
						</div>

						<div className="space-y-2">
							<Label>Payout Type *</Label>
							<Select
								value={form.watch("payoutType")}
								onValueChange={(v) => form.setValue("payoutType", v)}>
								<SelectTrigger>
									<SelectValue placeholder="Select type" />
								</SelectTrigger>
								<SelectContent>
									{Object.entries(payoutTypeLabels).map(([v, l]) => (
										<SelectItem key={v} value={v}>
											{l}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
							{form.formState.errors.payoutType && (
								<p className="text-xs text-red-500">
									{form.formState.errors.payoutType.message}
								</p>
							)}
						</div>

						<div className="grid grid-cols-2 gap-4">
							<div className="space-y-2">
								<Label>Base Rate (CAD) *</Label>
								<Input
									type="number"
									step="0.01"
									placeholder="0.00"
									{...form.register("baseRate")}
								/>
							</div>
							<div className="space-y-2">
								<Label>Min Guaranteed (CAD)</Label>
								<Input
									type="number"
									step="0.01"
									placeholder="Optional"
									{...form.register("minGuaranteed")}
								/>
							</div>
						</div>

						{/* Hybrid bonus fields */}
						{isHybrid && (
							<div className="grid grid-cols-2 gap-4 p-3 bg-slate-50 rounded-lg">
								<div className="space-y-2">
									<Label className="text-xs">Bonus per delivery</Label>
									<Input
										type="number"
										step="0.01"
										className="h-8"
										{...form.register("bonusPerDelivery")}
									/>
								</div>
								<div className="space-y-2">
									<Label className="text-xs">
										Above threshold (deliveries)
									</Label>
									<Input
										type="number"
										className="h-8"
										{...form.register("bonusThreshold")}
									/>
								</div>
							</div>
						)}

						<div className="flex justify-end gap-2 pt-2">
							<Button
								type="button"
								variant="outline"
								onClick={() => setDialogOpen(false)}>
								Cancel
							</Button>
							<Button
								type="submit"
								className="bg-blue-600 hover:bg-blue-700"
								disabled={createPolicy.isPending}>
								{createPolicy.isPending ? "Creating..." : "Create Policy"}
							</Button>
						</div>
					</form>
				</DialogContent>
			</Dialog>
		</div>
	);
}
