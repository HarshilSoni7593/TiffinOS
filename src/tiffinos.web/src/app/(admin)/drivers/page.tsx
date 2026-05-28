"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { Driver, PayoutPolicy } from "@/types/api";
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
import { Plus, Users, Bike, Car, Truck } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { cn } from "@/lib/utils";

// ── Schemas ───────────────────────────────────────────────────
const policySchema = z.object({
	name: z.string().min(1, "Name is required"),
	payoutType: z.string().min(1, "Payout type is required"),
	baseRate: z.coerce.number().min(0),
	bonusPerDelivery: z.coerce.number().optional(),
	bonusThreshold: z.coerce.number().optional(),
	minGuaranteed: z.coerce.number().optional(),
	currency: z.string().default("CAD"),
});

const driverSchema = z.object({
	email: z.string().email("Invalid email"),
	password: z.string().min(6, "Min 6 characters"),
	firstName: z.string().min(1, "Required"),
	lastName: z.string().min(1, "Required"),
	phone: z.string().min(1, "Required"),
	vehicleType: z.string().min(1, "Required"),
	licenceNumber: z.string().optional(),
	maxDeliveriesPerDay: z.coerce.number().min(1).default(50),
	payoutPolicyId: z.string().optional(),
});

type PolicyForm = z.infer<typeof policySchema>;
type DriverForm = z.infer<typeof driverSchema>;

const vehicleIcons: Record<string, React.ReactNode> = {
	bike: <Bike size={14} />,
	scooter: <Bike size={14} />,
	car: <Car size={14} />,
	cycle: <Bike size={14} />,
};

export default function DriversPage() {
	const queryClient = useQueryClient();
	const [activeTab, setActiveTab] = useState<"drivers" | "policies">("drivers");
	const [driverDialogOpen, setDriverDialog] = useState(false);
	const [policyDialogOpen, setPolicyDialog] = useState(false);

	// ── Queries ───────────────────────────────────────────────
	const { data: drivers = [], isLoading: loadingDrivers } = useQuery<Driver[]>({
		queryKey: ["drivers"],
		queryFn: () => api.get("/api/drivers").then((r) => r.data),
	});

	const { data: policies = [], isLoading: loadingPolicies } = useQuery<
		PayoutPolicy[]
	>({
		queryKey: ["payoutPolicies"],
		queryFn: () => api.get("/api/drivers/payout-policies").then((r) => r.data),
	});

	// ── Mutations ─────────────────────────────────────────────
	const createDriver = useMutation({
		mutationFn: (data: DriverForm) =>
			api.post("/api/drivers", {
				...data,
				payoutPolicyId: data.payoutPolicyId || null,
				licenceNumber: data.licenceNumber || null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["drivers"] });
			setDriverDialog(false);
			toast.success("Driver created successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create driver"),
	});

	const createPolicy = useMutation({
		mutationFn: (data: PolicyForm) =>
			api.post("/api/drivers/payout-policies", data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["payoutPolicies"] });
			setPolicyDialog(false);
			toast.success("Payout policy created");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create policy"),
	});

	const deactivateDriver = useMutation({
		mutationFn: (id: string) => api.delete(`/api/drivers/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["drivers"] });
			toast.success("Driver deactivated");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to deactivate driver"),
	});

	return (
		<div className="space-y-6">
			{/* Tabs + Actions */}
			<div className="flex items-center justify-between">
				<div className="flex gap-2">
					<Button
						variant={activeTab === "drivers" ? "default" : "outline"}
						size="sm"
						onClick={() => setActiveTab("drivers")}>
						Drivers ({drivers.length})
					</Button>
					<Button
						variant={activeTab === "policies" ? "default" : "outline"}
						size="sm"
						onClick={() => setActiveTab("policies")}>
						Payout Policies ({policies.length})
					</Button>
				</div>
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={() =>
						activeTab === "drivers"
							? setDriverDialog(true)
							: setPolicyDialog(true)
					}>
					<Plus size={16} className="mr-2" />
					{activeTab === "drivers" ? "Add Driver" : "Add Policy"}
				</Button>
			</div>

			{/* Drivers Tab */}
			{activeTab === "drivers" &&
				(loadingDrivers ? (
					<div className="space-y-3">
						{Array.from({ length: 3 }).map((_, i) => (
							<Skeleton key={i} className="h-20 w-full rounded-xl" />
						))}
					</div>
				) : drivers.length === 0 ? (
					<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
						<Users size={32} className="text-slate-300 mx-auto mb-3" />
						<p className="text-slate-400 text-sm">
							No drivers yet. Add your first driver.
						</p>
						<Button
							size="sm"
							className="mt-4 bg-blue-600 hover:bg-blue-700"
							onClick={() => setDriverDialog(true)}>
							<Plus size={16} className="mr-2" />
							Add Driver
						</Button>
					</div>
				) : (
					<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
						<table className="w-full">
							<thead>
								<tr className="border-b border-slate-100 bg-slate-50">
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Driver
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Vehicle
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Payout Policy
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Today's Deliveries
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Status
									</th>
									<th className="px-5 py-3" />
								</tr>
							</thead>
							<tbody>
								{drivers.map((driver) => (
									<tr
										key={driver.id}
										className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
										<td className="px-5 py-4">
											<div className="flex items-center gap-3">
												<div className="w-8 h-8 rounded-full bg-slate-200 flex items-center justify-center flex-shrink-0">
													<span className="text-xs font-semibold text-slate-600">
														{driver.fullName.charAt(0)}
													</span>
												</div>
												<span className="text-sm font-medium text-slate-800">
													{driver.fullName}
												</span>
											</div>
										</td>
										<td className="px-5 py-4">
											<div className="flex items-center gap-1.5 text-sm text-slate-600 capitalize">
												{vehicleIcons[driver.vehicleType] ?? (
													<Truck size={14} />
												)}
												{driver.vehicleType}
											</div>
										</td>
										<td className="px-5 py-4 text-sm text-slate-600">
											{driver.payoutPolicyName ?? (
												<span className="text-amber-600 text-xs">
													No policy assigned
												</span>
											)}
										</td>
										<td className="px-5 py-4">
											<span
												className={cn(
													"text-sm font-medium",
													driver.todayDeliveries > 0
														? "text-blue-600"
														: "text-slate-400",
												)}>
												{driver.todayDeliveries}
											</span>
										</td>
										<td className="px-5 py-4">
											<Badge
												variant="secondary"
												className={cn(
													"text-xs",
													driver.isAvailable
														? "bg-green-100 text-green-700"
														: "bg-slate-100 text-slate-500",
												)}>
												{driver.isAvailable ? "Available" : "Inactive"}
											</Badge>
										</td>
										<td className="px-5 py-4 text-right">
											{driver.isAvailable && (
												<Button
													variant="outline"
													size="sm"
													className="h-7 text-xs text-red-600 border-red-200 hover:bg-red-50"
													onClick={() => {
														if (confirm(`Deactivate ${driver.fullName}?`))
															deactivateDriver.mutate(driver.id);
													}}>
													Deactivate
												</Button>
											)}
										</td>
									</tr>
								))}
							</tbody>
						</table>
					</div>
				))}

			{/* Payout Policies Tab */}
			{activeTab === "policies" &&
				(loadingPolicies ? (
					<Skeleton className="h-40 w-full rounded-xl" />
				) : (
					<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
						{policies.length === 0 ? (
							<div className="p-12 text-center">
								<p className="text-slate-400 text-sm">
									No payout policies yet.
								</p>
							</div>
						) : (
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
											<td className="px-5 py-4 text-sm font-medium text-slate-800">
												{policy.name}
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
												{policy.minGuaranteed
													? `$${policy.minGuaranteed}`
													: "—"}
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
						)}
					</div>
				))}

			{/* Driver Dialog */}
			<DriverDialog
				open={driverDialogOpen}
				onClose={() => setDriverDialog(false)}
				policies={policies}
				onSubmit={(data) => createDriver.mutate(data)}
				loading={createDriver.isPending}
			/>

			{/* Policy Dialog */}
			<PolicyDialog
				open={policyDialogOpen}
				onClose={() => setPolicyDialog(false)}
				onSubmit={(data) => createPolicy.mutate(data)}
				loading={createPolicy.isPending}
			/>
		</div>
	);
}

// ── Driver Dialog ─────────────────────────────────────────────
function DriverDialog({
	open,
	onClose,
	policies,
	onSubmit,
	loading,
}: {
	open: boolean;
	onClose: () => void;
	policies: PayoutPolicy[];
	onSubmit: (data: DriverForm) => void;
	loading: boolean;
}) {
	const form = useForm<DriverForm>({
		resolver: zodResolver(driverSchema),
		defaultValues: {
			email: "",
			password: "",
			firstName: "",
			lastName: "",
			phone: "",
			vehicleType: "",
			maxDeliveriesPerDay: 50,
		},
	});

	return (
		<Dialog open={open} onOpenChange={onClose}>
			<DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
				<DialogHeader>
					<DialogTitle>Add Driver</DialogTitle>
				</DialogHeader>
				<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 pt-2">
					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>First Name *</Label>
							<Input {...form.register("firstName")} />
							{form.formState.errors.firstName && (
								<p className="text-xs text-red-500">
									{form.formState.errors.firstName.message}
								</p>
							)}
						</div>
						<div className="space-y-2">
							<Label>Last Name *</Label>
							<Input {...form.register("lastName")} />
						</div>
					</div>

					<div className="space-y-2">
						<Label>Email *</Label>
						<Input
							type="email"
							placeholder="driver@restaurant.ca"
							{...form.register("email")}
						/>
						{form.formState.errors.email && (
							<p className="text-xs text-red-500">
								{form.formState.errors.email.message}
							</p>
						)}
					</div>

					<div className="space-y-2">
						<Label>Password *</Label>
						<Input
							type="password"
							placeholder="Min 6 characters"
							{...form.register("password")}
						/>
					</div>

					<div className="space-y-2">
						<Label>Phone *</Label>
						<Input placeholder="+1 647 555 0000" {...form.register("phone")} />
					</div>

					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Vehicle Type *</Label>
							<Select
								value={form.watch("vehicleType")}
								onValueChange={(v) => form.setValue("vehicleType", v)}>
								<SelectTrigger>
									<SelectValue placeholder="Select" />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value="bike">Bike</SelectItem>
									<SelectItem value="scooter">Scooter</SelectItem>
									<SelectItem value="car">Car</SelectItem>
									<SelectItem value="cycle">Cycle</SelectItem>
								</SelectContent>
							</Select>
						</div>
						<div className="space-y-2">
							<Label>Max Deliveries/Day</Label>
							<Input
								type="number"
								defaultValue={50}
								{...form.register("maxDeliveriesPerDay")}
							/>
						</div>
					</div>

					<div className="space-y-2">
						<Label>Licence Number</Label>
						<Input placeholder="Optional" {...form.register("licenceNumber")} />
					</div>

					<div className="space-y-2">
						<Label>Payout Policy</Label>
						<Select
							value={form.watch("payoutPolicyId") ?? ""}
							onValueChange={(v) => form.setValue("payoutPolicyId", v)}>
							<SelectTrigger>
								<SelectValue placeholder="Select policy (optional)" />
							</SelectTrigger>
							<SelectContent>
								{policies.map((p) => (
									<SelectItem key={p.id} value={p.id}>
										{p.name} — {p.payoutType}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
					</div>

					<div className="flex justify-end gap-2 pt-2">
						<Button type="button" variant="outline" onClick={onClose}>
							Cancel
						</Button>
						<Button
							type="submit"
							className="bg-blue-600 hover:bg-blue-700"
							disabled={loading}>
							{loading ? "Creating..." : "Create Driver"}
						</Button>
					</div>
				</form>
			</DialogContent>
		</Dialog>
	);
}

// ── Policy Dialog ─────────────────────────────────────────────
function PolicyDialog({
	open,
	onClose,
	onSubmit,
	loading,
}: {
	open: boolean;
	onClose: () => void;
	onSubmit: (data: PolicyForm) => void;
	loading: boolean;
}) {
	const form = useForm<PolicyForm>({
		resolver: zodResolver(policySchema),
		defaultValues: {
			name: "",
			payoutType: "",
			baseRate: 0,
			currency: "CAD",
		},
	});

	const isHybrid = form.watch("payoutType") === "hybrid";

	return (
		<Dialog open={open} onOpenChange={onClose}>
			<DialogContent className="sm:max-w-md">
				<DialogHeader>
					<DialogTitle>Add Payout Policy</DialogTitle>
				</DialogHeader>
				<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 pt-2">
					<div className="space-y-2">
						<Label>Policy Name *</Label>
						<Input
							placeholder="e.g. Standard Driver Rate"
							{...form.register("name")}
						/>
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
								<SelectItem value="per_delivery">Per Delivery</SelectItem>
								<SelectItem value="per_day">Per Day (flat)</SelectItem>
								<SelectItem value="per_km">Per Kilometre</SelectItem>
								<SelectItem value="per_zone">Per Zone</SelectItem>
								<SelectItem value="hybrid">Hybrid (base + bonus)</SelectItem>
							</SelectContent>
						</Select>
					</div>

					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Base Rate (CAD) *</Label>
							<Input type="number" step="0.01" {...form.register("baseRate")} />
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
								<Label className="text-xs">Above threshold (deliveries)</Label>
								<Input
									type="number"
									className="h-8"
									{...form.register("bonusThreshold")}
								/>
							</div>
						</div>
					)}

					<div className="flex justify-end gap-2 pt-2">
						<Button type="button" variant="outline" onClick={onClose}>
							Cancel
						</Button>
						<Button
							type="submit"
							className="bg-blue-600 hover:bg-blue-700"
							disabled={loading}>
							{loading ? "Creating..." : "Create Policy"}
						</Button>
					</div>
				</form>
			</DialogContent>
		</Dialog>
	);
}
