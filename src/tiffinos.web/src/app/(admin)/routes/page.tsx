"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { DispatchList, Driver, Zone } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import {
	Route,
	RefreshCw,
	MapPin,
	User,
	CheckCircle,
	Clock,
	AlertCircle,
} from "lucide-react";
import { cn } from "@/lib/utils";

const today = new Date().toISOString().split("T")[0];

const statusConfig: Record<
	string,
	{ label: string; className: string; icon: React.ReactNode }
> = {
	pending: {
		label: "Pending",
		className: "bg-amber-100 text-amber-700",
		icon: <Clock size={12} />,
	},
	assigned: {
		label: "Assigned",
		className: "bg-blue-100 text-blue-700",
		icon: <User size={12} />,
	},
	in_progress: {
		label: "In Progress",
		className: "bg-purple-100 text-purple-700",
		icon: <Route size={12} />,
	},
	delivered: {
		label: "Delivered",
		className: "bg-green-100 text-green-700",
		icon: <CheckCircle size={12} />,
	},
	attempted: {
		label: "Attempted",
		className: "bg-red-100 text-red-700",
		icon: <AlertCircle size={12} />,
	},
	skipped: {
		label: "Skipped",
		className: "bg-slate-100 text-slate-500",
		icon: <Clock size={12} />,
	},
};

export default function RoutesPage() {
	const queryClient = useQueryClient();
	const [expandedZone, setExpandedZone] = useState<string | null>(null);
	const [assigningZone, setAssigningZone] = useState<string | null>(null);
	const [selectedDriver, setSelectedDriver] = useState<Record<string, string>>(
		{},
	);

	// ── Queries ───────────────────────────────────────────────
	const {
		data: dispatch,
		isLoading: loadingDispatch,
		refetch,
	} = useQuery<DispatchList>({
		queryKey: ["dispatch", today],
		queryFn: () =>
			api.get("/api/delivery-engine/dispatch-list").then((r) => r.data),
	});

	const { data: drivers = [] } = useQuery<Driver[]>({
		queryKey: ["drivers"],
		queryFn: () => api.get("/api/drivers").then((r) => r.data),
	});

	// ── Mutations ─────────────────────────────────────────────
	const generateDispatch = useMutation({
		mutationFn: () => api.post("/api/delivery-engine/generate-dispatch-list"),
		onSuccess: () => {
			setTimeout(() => {
				queryClient.invalidateQueries({ queryKey: ["dispatch"] });
				refetch();
			}, 2000);
			toast.success("Dispatch list generation queued. Refreshing in 2s...");
		},
		onError: () => toast.error("Failed to generate dispatch list"),
	});

	const assignDriver = useMutation({
		mutationFn: ({ driverId, zoneId }: { driverId: string; zoneId: string }) =>
			api.post("/api/routes/assign", {
				driverId,
				zoneId,
				assignmentDate: today,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["dispatch"] });
			refetch();
			setAssigningZone(null);
			toast.success("Driver assigned and route optimised successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to assign driver"),
	});

	const availableDrivers = drivers.filter((d) => d.isAvailable);

	return (
		<div className="space-y-6">
			{/* Header Actions */}
			<div className="flex items-center justify-between">
				<div>
					<p className="text-sm text-slate-500">
						{today} · {dispatch?.total ?? 0} total deliveries
					</p>
				</div>
				<div className="flex gap-2">
					<Button variant="outline" size="sm" onClick={() => refetch()}>
						<RefreshCw size={14} className="mr-2" />
						Refresh
					</Button>
					<Button
						size="sm"
						className="bg-blue-600 hover:bg-blue-700"
						onClick={() => generateDispatch.mutate()}
						disabled={generateDispatch.isPending}>
						{generateDispatch.isPending
							? "Generating..."
							: "Generate Dispatch List"}
					</Button>
				</div>
			</div>

			{/* Dispatch List */}
			{loadingDispatch ? (
				<div className="space-y-3">
					{Array.from({ length: 2 }).map((_, i) => (
						<Skeleton key={i} className="h-32 w-full rounded-xl" />
					))}
				</div>
			) : !dispatch || dispatch.total === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<Route size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						No deliveries scheduled for today.
					</p>
					<p className="text-slate-400 text-xs mt-1">
						Click "Generate Dispatch List" to create today's delivery schedule.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={() => generateDispatch.mutate()}
						disabled={generateDispatch.isPending}>
						Generate Now
					</Button>
				</div>
			) : (
				<div className="space-y-4">
					{dispatch.zones.map((zone) => {
						const delivered = zone.deliveries.filter(
							(d) => d.status === "delivered",
						).length;
						const pending = zone.deliveries.filter(
							(d) => d.status === "pending",
						).length;
						const assigned = zone.deliveries.filter(
							(d) => d.status === "assigned",
						).length;
						const inProgress = zone.deliveries.filter(
							(d) => d.status === "in_progress",
						).length;
						const isExpanded = expandedZone === zone.zoneId;
						const isAssigning = assigningZone === zone.zoneId;
						const hasDriver = zone.deliveries.some((d) => d.driverId);
						const progress =
							zone.totalDeliveries > 0
								? Math.round((delivered / zone.totalDeliveries) * 100)
								: 0;

						return (
							<div
								key={zone.zoneId}
								className="bg-white rounded-xl border border-slate-200 overflow-hidden">
								{/* Zone Header */}
								<div className="px-5 py-4">
									<div className="flex items-center justify-between">
										<div className="flex items-center gap-3">
											<div className="w-8 h-8 rounded-lg bg-blue-100 flex items-center justify-center">
												<MapPin size={16} className="text-blue-600" />
											</div>
											<div>
												<p className="font-medium text-slate-800">
													{zone.zoneName}
												</p>
												<p className="text-xs text-slate-400">
													{zone.zoneCode} · {zone.totalDeliveries} deliveries
												</p>
											</div>
										</div>

										<div className="flex items-center gap-3">
											{/* Progress */}
											<div className="text-right hidden sm:block">
												<p className="text-xs text-slate-500">
													{delivered}/{zone.totalDeliveries} delivered
												</p>
												<div className="w-24 h-1.5 bg-slate-100 rounded-full mt-1">
													<div
														className="h-full bg-green-500 rounded-full transition-all"
														style={{ width: `${progress}%` }}
													/>
												</div>
											</div>

											{/* Status badges */}
											<div className="flex gap-1">
												{pending > 0 && (
													<Badge
														variant="secondary"
														className="bg-amber-100 text-amber-700 text-xs">
														{pending} pending
													</Badge>
												)}
												{assigned > 0 && (
													<Badge
														variant="secondary"
														className="bg-blue-100 text-blue-700 text-xs">
														{assigned} assigned
													</Badge>
												)}
												{inProgress > 0 && (
													<Badge
														variant="secondary"
														className="bg-purple-100 text-purple-700 text-xs">
														{inProgress} in progress
													</Badge>
												)}
												{delivered > 0 && (
													<Badge
														variant="secondary"
														className="bg-green-100 text-green-700 text-xs">
														{delivered} done
													</Badge>
												)}
											</div>

											{/* Assign Driver */}
											{pending > 0 && !hasDriver && (
												<Button
													size="sm"
													className="h-8 text-xs bg-blue-600 hover:bg-blue-700"
													onClick={() =>
														setAssigningZone(isAssigning ? null : zone.zoneId)
													}>
													Assign Driver
												</Button>
											)}

											{/* Expand */}
											<button
												onClick={() =>
													setExpandedZone(isExpanded ? null : zone.zoneId)
												}
												className="text-xs text-slate-400 hover:text-slate-600">
												{isExpanded ? "Hide" : "View all"}
											</button>
										</div>
									</div>

									{/* Driver Assignment Row */}
									{isAssigning && (
										<div className="mt-4 p-3 bg-blue-50 rounded-lg flex items-center gap-3">
											<Select
												value={selectedDriver[zone.zoneId] ?? ""}
												onValueChange={(v) =>
													setSelectedDriver((prev) => ({
														...prev,
														[zone.zoneId]: v,
													}))
												}>
												<SelectTrigger className="flex-1 h-8 text-sm">
													<SelectValue placeholder="Select a driver" />
												</SelectTrigger>
												<SelectContent>
													{availableDrivers.map((d) => (
														<SelectItem key={d.id} value={d.id}>
															{d.fullName} — {d.vehicleType}
														</SelectItem>
													))}
												</SelectContent>
											</Select>
											<Button
												size="sm"
												className="h-8 text-xs bg-blue-600 hover:bg-blue-700"
												disabled={
													!selectedDriver[zone.zoneId] || assignDriver.isPending
												}
												onClick={() => {
													if (selectedDriver[zone.zoneId])
														assignDriver.mutate({
															driverId: selectedDriver[zone.zoneId],
															zoneId: zone.zoneId,
														});
												}}>
												{assignDriver.isPending
													? "Assigning..."
													: "Confirm & Optimise Route"}
											</Button>
											<button
												onClick={() => setAssigningZone(null)}
												className="text-xs text-slate-400 hover:text-slate-600">
												Cancel
											</button>
										</div>
									)}
								</div>

								{/* Delivery List */}
								{isExpanded && (
									<div className="border-t border-slate-100">
										<table className="w-full">
											<thead>
												<tr className="bg-slate-50">
													<th className="text-left text-xs font-medium text-slate-500 px-5 py-2">
														#
													</th>
													<th className="text-left text-xs font-medium text-slate-500 px-5 py-2">
														Customer
													</th>
													<th className="text-left text-xs font-medium text-slate-500 px-5 py-2">
														Address
													</th>
													<th className="text-left text-xs font-medium text-slate-500 px-5 py-2">
														Plan
													</th>
													<th className="text-left text-xs font-medium text-slate-500 px-5 py-2">
														Status
													</th>
												</tr>
											</thead>
											<tbody>
												{zone.deliveries
													.sort(
														(a, b) =>
															(a.sequenceNumber ?? 999) -
															(b.sequenceNumber ?? 999),
													)
													.map((delivery, idx) => {
														const status = statusConfig[delivery.status] ?? {
															label: delivery.status,
															className: "bg-slate-100 text-slate-500",
															icon: null,
														};
														return (
															<tr
																key={delivery.scheduleId}
																className="border-t border-slate-50 hover:bg-slate-50">
																<td className="px-5 py-3 text-xs text-slate-400">
																	{delivery.sequenceNumber ?? idx + 1}
																</td>
																<td className="px-5 py-3">
																	<p className="text-sm font-medium text-slate-800">
																		{delivery.customerName}
																	</p>
																	{delivery.customerPhone && (
																		<p className="text-xs text-slate-400">
																			{delivery.customerPhone}
																		</p>
																	)}
																</td>
																<td className="px-5 py-3">
																	<p className="text-sm text-slate-600">
																		{delivery.deliveryAddress}
																	</p>
																	{delivery.floorOrUnit && (
																		<p className="text-xs text-slate-400">
																			{delivery.floorOrUnit}
																		</p>
																	)}
																	{delivery.deliveryInstructions && (
																		<p className="text-xs text-amber-600">
																			📝 {delivery.deliveryInstructions}
																		</p>
																	)}
																	{delivery.spicePreference && (
																		<p className="text-xs text-slate-400">
																			🌶 {delivery.spicePreference}
																		</p>
																	)}
																</td>
																<td className="px-5 py-3 text-sm text-slate-600">
																	{delivery.planName}
																</td>
																<td className="px-5 py-3">
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
															</tr>
														);
													})}
											</tbody>
										</table>
									</div>
								)}
							</div>
						);
					})}
				</div>
			)}
		</div>
	);
}
