"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { Subscription } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import {
	Search,
	Package,
	CheckCircle,
	PauseCircle,
	XCircle,
	Clock,
} from "lucide-react";
import { cn } from "@/lib/utils";

const statusConfig: Record<
	string,
	{
		label: string;
		className: string;
		icon: React.ReactNode;
	}
> = {
	pending: {
		label: "Pending",
		className: "bg-amber-100 text-amber-700",
		icon: <Clock size={12} />,
	},
	active: {
		label: "Active",
		className: "bg-green-100 text-green-700",
		icon: <CheckCircle size={12} />,
	},
	paused: {
		label: "Paused",
		className: "bg-blue-100 text-blue-700",
		icon: <PauseCircle size={12} />,
	},
	cancelled: {
		label: "Cancelled",
		className: "bg-red-100 text-red-700",
		icon: <XCircle size={12} />,
	},
	expired: {
		label: "Expired",
		className: "bg-slate-100 text-slate-500",
		icon: <XCircle size={12} />,
	},
};

export default function SubscriptionsPage() {
	const queryClient = useQueryClient();
	const [search, setSearch] = useState("");
	const [statusFilter, setStatusFilter] = useState("all");

	const { data: subscriptions = [], isLoading } = useQuery<Subscription[]>({
		queryKey: ["subscriptions", statusFilter],
		queryFn: () =>
			api
				.get("/api/subscriptions", {
					params: statusFilter !== "all" ? { status: statusFilter } : {},
				})
				.then((r) => r.data),
	});

	const activateSub = useMutation({
		mutationFn: (id: string) => api.post(`/api/subscriptions/${id}/activate`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
			toast.success("Subscription activated");
		},
		onError: () => toast.error("Failed to activate subscription"),
	});

	const cancelSub = useMutation({
		mutationFn: (id: string) =>
			api.post(`/api/subscriptions/${id}/cancel`, {
				reason: "Cancelled by admin",
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
			toast.success("Subscription cancelled");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to cancel"),
	});

	// Filter by search
	const filtered = subscriptions.filter(
		(s) =>
			s.customerName.toLowerCase().includes(search.toLowerCase()) ||
			s.planName.toLowerCase().includes(search.toLowerCase()) ||
			s.zoneName.toLowerCase().includes(search.toLowerCase()),
	);

	// Summary counts
	const counts = {
		all: subscriptions.length,
		active: subscriptions.filter((s) => s.status === "active").length,
		pending: subscriptions.filter((s) => s.status === "pending").length,
		paused: subscriptions.filter((s) => s.status === "paused").length,
		cancelled: subscriptions.filter((s) => s.status === "cancelled").length,
	};

	return (
		<div className="space-y-6">
			{/* Summary Cards */}
			<div className="grid grid-cols-4 gap-4">
				{[
					{ label: "Active", count: counts.active, color: "text-green-600" },
					{ label: "Pending", count: counts.pending, color: "text-amber-600" },
					{ label: "Paused", count: counts.paused, color: "text-blue-600" },
					{
						label: "Cancelled",
						count: counts.cancelled,
						color: "text-red-600",
					},
				].map((item) => (
					<div
						key={item.label}
						className="bg-white rounded-xl border border-slate-200 p-4 cursor-pointer hover:border-slate-300 transition-colors"
						onClick={() => setStatusFilter(item.label.toLowerCase())}>
						<p className="text-xs text-slate-500">{item.label}</p>
						<p className={cn("text-2xl font-bold mt-1", item.color)}>
							{item.count}
						</p>
					</div>
				))}
			</div>

			{/* Filters */}
			<div className="flex items-center gap-3">
				<div className="relative flex-1 max-w-sm">
					<Search
						size={14}
						className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
					/>
					<Input
						placeholder="Search by customer, plan, zone..."
						className="pl-9 h-9"
						value={search}
						onChange={(e) => setSearch(e.target.value)}
					/>
				</div>
				<Select value={statusFilter} onValueChange={setStatusFilter}>
					<SelectTrigger className="w-36 h-9">
						<SelectValue />
					</SelectTrigger>
					<SelectContent>
						<SelectItem value="all">All Status</SelectItem>
						<SelectItem value="active">Active</SelectItem>
						<SelectItem value="pending">Pending</SelectItem>
						<SelectItem value="paused">Paused</SelectItem>
						<SelectItem value="cancelled">Cancelled</SelectItem>
						<SelectItem value="expired">Expired</SelectItem>
					</SelectContent>
				</Select>
			</div>

			{/* Table */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 5 }).map((_, i) => (
						<Skeleton key={i} className="h-16 w-full rounded-xl" />
					))}
				</div>
			) : filtered.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<Package size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						{search || statusFilter !== "all"
							? "No subscriptions match your filters."
							: "No subscriptions yet."}
					</p>
				</div>
			) : (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					<table className="w-full">
						<thead>
							<tr className="border-b border-slate-100 bg-slate-50">
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Customer
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Plan
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Zone
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Period
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Amount
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Status
								</th>
								<th className="px-5 py-3" />
							</tr>
						</thead>
						<tbody>
							{filtered.map((sub) => {
								const status = statusConfig[sub.status] ?? {
									label: sub.status,
									className: "bg-slate-100 text-slate-500",
									icon: null,
								};
								return (
									<tr
										key={sub.id}
										className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
										<td className="px-5 py-4">
											<p className="text-sm font-medium text-slate-800">
												{sub.customerName}
											</p>
										</td>
										<td className="px-5 py-4">
											<p className="text-sm text-slate-700">{sub.planName}</p>
											<p className="text-xs text-slate-400 capitalize">
												{sub.durationType}
											</p>
										</td>
										<td className="px-5 py-4 text-sm text-slate-600">
											{sub.zoneName}
										</td>
										<td className="px-5 py-4">
											<p className="text-xs text-slate-600">{sub.startDate}</p>
											{sub.endDate && (
												<p className="text-xs text-slate-400">
													→ {sub.endDate}
												</p>
											)}
										</td>
										<td className="px-5 py-4">
											<p className="text-sm font-medium text-slate-800">
												${sub.lockedTotalAmount}
											</p>
											{sub.lockedDeliveryCharge > 0 && (
												<p className="text-xs text-slate-400">
													+${sub.lockedDeliveryCharge} delivery
												</p>
											)}
											{sub.lockedDeliveryCharge === 0 && (
												<p className="text-xs text-green-600">Free delivery</p>
											)}
										</td>
										<td className="px-5 py-4">
											<Badge
												variant="secondary"
												className={cn(
													"text-xs flex items-center gap-1 w-fit",
													status.className,
												)}>
												{status.icon}
												{status.label}
											</Badge>
										</td>
										<td className="px-5 py-4">
											<div className="flex items-center justify-end gap-2">
												{sub.status === "pending" && (
													<Button
														size="sm"
														className="h-7 text-xs bg-green-600 hover:bg-green-700"
														onClick={() => activateSub.mutate(sub.id)}>
														Activate
													</Button>
												)}
												{(sub.status === "active" ||
													sub.status === "paused") && (
													<Button
														size="sm"
														variant="outline"
														className="h-7 text-xs text-red-600 border-red-200 hover:bg-red-50"
														onClick={() => {
															if (
																confirm(
																	`Cancel ${sub.customerName}'s subscription?`,
																)
															)
																cancelSub.mutate(sub.id);
														}}>
														Cancel
													</Button>
												)}
											</div>
										</td>
									</tr>
								);
							})}
						</tbody>
					</table>
				</div>
			)}
		</div>
	);
}
