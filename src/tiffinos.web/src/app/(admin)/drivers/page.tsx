"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
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
import { Plus, Users, Bike, Car, Truck, Search } from "lucide-react";
import { useForm as useFormHook } from "react-hook-form";
import { cn } from "@/lib/utils";

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

type DriverForm = z.infer<typeof driverSchema>;

const vehicleConfig: Record<
	string,
	{
		icon: React.ReactNode;
		label: string;
		color: string;
	}
> = {
	bike: {
		icon: <Bike size={14} />,
		label: "Bike",
		color: "bg-green-100 text-green-700",
	},
	scooter: {
		icon: <Bike size={14} />,
		label: "Scooter",
		color: "bg-blue-100 text-blue-700",
	},
	car: {
		icon: <Car size={14} />,
		label: "Car",
		color: "bg-purple-100 text-purple-700",
	},
	cycle: {
		icon: <Bike size={14} />,
		label: "Cycle",
		color: "bg-amber-100 text-amber-700",
	},
	truck: {
		icon: <Truck size={14} />,
		label: "Truck",
		color: "bg-red-100 text-red-700",
	},
};

export default function DriversPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);
	const [search, setSearch] = useState("");

	const { data: drivers = [], isLoading } = useQuery<Driver[]>({
		queryKey: ["drivers"],
		queryFn: () => api.get("/api/drivers").then((r) => r.data),
	});

	const { data: policies = [] } = useQuery<PayoutPolicy[]>({
		queryKey: ["payoutPolicies"],
		queryFn: () => api.get("/api/drivers/payout-policies").then((r) => r.data),
	});

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

	const createDriver = useMutation({
		mutationFn: (data: DriverForm) =>
			api.post("/api/drivers", {
				...data,
				payoutPolicyId: data.payoutPolicyId || null,
				licenceNumber: data.licenceNumber || null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["drivers"] });
			setDialogOpen(false);
			form.reset();
			toast.success("Driver created successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create driver"),
	});

	const deactivateDriver = useMutation({
		mutationFn: (id: string) => api.delete(`/api/drivers/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["drivers"] });
			toast.success("Driver deactivated");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to deactivate"),
	});

	const filtered = drivers.filter(
		(d) =>
			d.fullName.toLowerCase().includes(search.toLowerCase()) ||
			d.vehicleType.toLowerCase().includes(search.toLowerCase()),
	);

	const available = drivers.filter((d) => d.isAvailable).length;
	const totalToday = drivers.reduce((sum, d) => sum + d.todayDeliveries, 0);

	return (
		<div className="space-y-6">
			{/* Summary Cards */}
			<div className="grid grid-cols-3 gap-4">
				<div className="bg-white rounded-xl border border-slate-200 p-4">
					<p className="text-xs text-slate-500">Total Drivers</p>
					<p className="text-2xl font-bold text-slate-800 mt-1">
						{drivers.length}
					</p>
				</div>
				<div className="bg-white rounded-xl border border-slate-200 p-4">
					<p className="text-xs text-slate-500">Available Today</p>
					<p className="text-2xl font-bold text-green-600 mt-1">{available}</p>
				</div>
				<div className="bg-white rounded-xl border border-slate-200 p-4">
					<p className="text-xs text-slate-500">Deliveries Today</p>
					<p className="text-2xl font-bold text-blue-600 mt-1">{totalToday}</p>
				</div>
			</div>

			{/* Search + Add */}
			<div className="flex items-center justify-between gap-4">
				<div className="relative flex-1 max-w-sm">
					<Search
						size={14}
						className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
					/>
					<Input
						placeholder="Search drivers..."
						className="pl-9 h-9"
						value={search}
						onChange={(e) => setSearch(e.target.value)}
					/>
				</div>
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={() => {
						form.reset();
						setDialogOpen(true);
					}}>
					<Plus size={16} className="mr-2" />
					Add Driver
				</Button>
			</div>

			{/* Drivers Grid */}
			{isLoading ? (
				<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-40 rounded-xl" />
					))}
				</div>
			) : filtered.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<Users size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						{search ? "No drivers match your search." : "No drivers yet."}
					</p>
					{!search && (
						<Button
							size="sm"
							className="mt-4 bg-blue-600 hover:bg-blue-700"
							onClick={() => setDialogOpen(true)}>
							<Plus size={16} className="mr-2" />
							Add First Driver
						</Button>
					)}
				</div>
			) : (
				<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
					{filtered.map((driver) => {
						const vehicle = vehicleConfig[driver.vehicleType] ?? {
							icon: <Truck size={14} />,
							label: driver.vehicleType,
							color: "bg-slate-100 text-slate-600",
						};

						return (
							<div
								key={driver.id}
								className="bg-white rounded-xl border border-slate-200 p-5 space-y-4">
								{/* Driver header */}
								<div className="flex items-start justify-between">
									<div className="flex items-center gap-3">
										<div className="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0">
											<span className="text-sm font-bold text-blue-600">
												{driver.fullName.charAt(0)}
											</span>
										</div>
										<div>
											<p className="font-semibold text-slate-800 text-sm">
												{driver.fullName}
											</p>
											<Badge
												variant="secondary"
												className={cn("text-xs mt-0.5", vehicle.color)}>
												<span className="mr-1">{vehicle.icon}</span>
												{vehicle.label}
											</Badge>
										</div>
									</div>
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
								</div>

								{/* Stats */}
								<div className="grid grid-cols-2 gap-3">
									<div className="bg-slate-50 rounded-lg p-3">
										<p className="text-xs text-slate-400">Today</p>
										<p
											className={cn(
												"text-xl font-bold mt-0.5",
												driver.todayDeliveries > 0
													? "text-blue-600"
													: "text-slate-300",
											)}>
											{driver.todayDeliveries}
										</p>
										<p className="text-xs text-slate-400">deliveries</p>
									</div>
									<div className="bg-slate-50 rounded-lg p-3">
										<p className="text-xs text-slate-400">Policy</p>
										<p className="text-xs font-medium text-slate-700 mt-0.5 leading-tight">
											{driver.payoutPolicyName ?? (
												<span className="text-amber-600">Not assigned</span>
											)}
										</p>
									</div>
								</div>

								{/* Actions */}
								{driver.isAvailable && (
									<Button
										variant="outline"
										size="sm"
										className="w-full h-8 text-xs text-red-600 border-red-200 hover:bg-red-50"
										onClick={() => {
											if (confirm(`Deactivate ${driver.fullName}?`))
												deactivateDriver.mutate(driver.id);
										}}>
										Deactivate Driver
									</Button>
								)}
							</div>
						);
					})}
				</div>
			)}

			{/* Add Driver Dialog */}
			<Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
				<DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
					<DialogHeader>
						<DialogTitle>Add Driver</DialogTitle>
					</DialogHeader>
					<form
						onSubmit={form.handleSubmit((d) => createDriver.mutate(d))}
						className="space-y-4 pt-2">
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
							<Input
								placeholder="+1 647 555 0000"
								{...form.register("phone")}
							/>
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
										{Object.entries(vehicleConfig).map(([v, c]) => (
											<SelectItem key={v} value={v}>
												{c.label}
											</SelectItem>
										))}
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
							<Input
								placeholder="Optional"
								{...form.register("licenceNumber")}
							/>
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
							<p className="text-xs text-slate-400">
								Manage policies in Setup → Payout Policies
							</p>
						</div>

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
								disabled={createDriver.isPending}>
								{createDriver.isPending ? "Creating..." : "Create Driver"}
							</Button>
						</div>
					</form>
				</DialogContent>
			</Dialog>
		</div>
	);
}
