"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import {
	PlanListItem,
	TiffinPlan,
	Category,
	MenuItem,
	PricingTier,
} from "@/types/api";
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
import {
	Plus,
	Pencil,
	Archive,
	ChevronDown,
	ChevronUp,
	Trash2,
	Check,
} from "lucide-react";
import { useForm, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { cn } from "@/lib/utils";

// ── Schemas ───────────────────────────────────────────────────
const pricingTierSchema = z.object({
	durationType: z.string().min(1),
	pricePerDay: z.coerce.number().min(0.01),
	billingCycleDays: z.coerce.number().min(1),
	totalAmount: z.coerce.number().min(0.01),
});

const planItemSchema = z.object({
	menuItemId: z.string().min(1),
	portionSize: z.string().min(1),
	quantity: z.coerce.number().min(0.1),
	displayOrder: z.coerce.number().min(0),
});

const planSchema = z.object({
	name: z.string().min(1, "Name is required"),
	description: z.string().optional(),
	dietaryType: z.string().min(1, "Dietary type is required"),
	boxType: z.string().optional(),
	allowSkip: z.boolean(),
	minSkipNoticeHours: z.coerce.number().optional(),
	maxSkipsPerCycle: z.coerce.number().optional(),
	skipCreditPolicy: z.string().optional(),
	expiryReminderDays: z.coerce.number().min(1),
	pricingTiers: z
		.array(pricingTierSchema)
		.min(1, "At least one pricing tier required"),
	items: z.array(planItemSchema).min(1, "At least one item required"),
});

type PlanForm = z.infer<typeof planSchema>;

// ── Billing cycle days by duration type ───────────────────────
const billingCycleDays: Record<string, number> = {
	daily: 1,
	weekly: 7,
	monthly: 30,
};

export default function PlansPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);
	const [expandedPlan, setExpandedPlan] = useState<string | null>(null);
	const [selectedPlan, setSelectedPlan] = useState<TiffinPlan | null>(null);

	// ── Queries ───────────────────────────────────────────────
	const { data: plans = [], isLoading } = useQuery<PlanListItem[]>({
		queryKey: ["plans"],
		queryFn: () => api.get("/api/plans").then((r) => r.data),
	});

	const { data: categories = [] } = useQuery<Category[]>({
		queryKey: ["categories"],
		queryFn: () => api.get("/api/menu-items/categories").then((r) => r.data),
	});

	const { data: menuItems = [] } = useQuery<MenuItem[]>({
		queryKey: ["menuItems"],
		queryFn: () => api.get("/api/menu-items").then((r) => r.data),
	});

	// Fetch full plan details when expanded
	const { data: planDetail } = useQuery<TiffinPlan>({
		queryKey: ["plan", expandedPlan],
		queryFn: () => api.get(`/api/plans/${expandedPlan}`).then((r) => r.data),
		enabled: !!expandedPlan,
	});

	// ── Mutations ─────────────────────────────────────────────
	const createPlan = useMutation({
		mutationFn: (data: PlanForm) =>
			api.post("/api/plans", {
				...data,
				boxType: data.boxType || null,
				description: data.description || null,
				skipCreditPolicy: data.skipCreditPolicy || null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["plans"] });
			setDialogOpen(false);
			toast.success("Plan created successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create plan"),
	});

	const archivePlan = useMutation({
		mutationFn: (id: string) => api.delete(`/api/plans/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["plans"] });
			toast.success("Plan archived successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to archive plan"),
	});

	const toggleExpand = (id: string) =>
		setExpandedPlan((prev) => (prev === id ? null : id));

	return (
		<div className="space-y-6">
			{/* Actions */}
			<div className="flex justify-end">
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={() => {
						setSelectedPlan(null);
						setDialogOpen(true);
					}}>
					<Plus size={16} className="mr-2" />
					Create Plan
				</Button>
			</div>

			{/* Plans List */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-20 w-full rounded-xl" />
					))}
				</div>
			) : plans.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<p className="text-slate-400 text-sm">
						No tiffin plans yet. Create your first plan.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={() => setDialogOpen(true)}>
						<Plus size={16} className="mr-2" />
						Create First Plan
					</Button>
				</div>
			) : (
				<div className="space-y-3">
					{plans.map((plan) => (
						<div
							key={plan.id}
							className="bg-white rounded-xl border border-slate-200 overflow-hidden">
							{/* Plan Header */}
							<div className="flex items-center justify-between px-5 py-4">
								<div
									className="flex items-center gap-4 flex-1 cursor-pointer"
									onClick={() => toggleExpand(plan.id)}>
									<div>
										<div className="flex items-center gap-2">
											<p className="font-medium text-slate-800">{plan.name}</p>
											<Badge
												variant="secondary"
												className={cn(
													"text-xs",
													plan.dietaryType === "veg"
														? "bg-green-100 text-green-700"
														: plan.dietaryType === "non_veg"
															? "bg-red-100 text-red-700"
															: "bg-amber-100 text-amber-700",
												)}>
												{plan.dietaryType === "veg"
													? "🌱 Veg"
													: plan.dietaryType === "non_veg"
														? "🍖 Non-veg"
														: "🔀 Both"}
											</Badge>
											<Badge
												variant="secondary"
												className={cn(
													"text-xs",
													plan.isActive
														? "bg-blue-100 text-blue-700"
														: "bg-slate-100 text-slate-500",
												)}>
												{plan.isActive ? "Active" : "Archived"}
											</Badge>
										</div>
										<p className="text-xs text-slate-400 mt-0.5">
											From ${plan.lowestPrice}/day · {plan.tierCount} tier
											{plan.tierCount !== 1 ? "s" : ""} · {plan.itemCount} item
											{plan.itemCount !== 1 ? "s" : ""}
										</p>
									</div>
								</div>
								<div className="flex items-center gap-2">
									{plan.isActive && (
										<Button
											variant="ghost"
											size="icon"
											className="h-8 w-8 text-red-500 hover:text-red-600 hover:bg-red-50"
											onClick={() => {
												if (
													confirm(
														`Archive "${plan.name}"? This cannot be undone if active subscriptions exist.`,
													)
												)
													archivePlan.mutate(plan.id);
											}}>
											<Archive size={14} />
										</Button>
									)}
									<button onClick={() => toggleExpand(plan.id)}>
										{expandedPlan === plan.id ? (
											<ChevronUp size={16} className="text-slate-400" />
										) : (
											<ChevronDown size={16} className="text-slate-400" />
										)}
									</button>
								</div>
							</div>

							{/* Plan Detail */}
							{expandedPlan === plan.id && planDetail && (
								<div className="border-t border-slate-100 px-5 py-4 space-y-4 bg-slate-50">
									{/* Pricing Tiers */}
									<div>
										<p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-2">
											Pricing Tiers
										</p>
										<div className="grid grid-cols-3 gap-3">
											{planDetail.pricingTiers.map((tier) => (
												<div
													key={tier.id}
													className="bg-white rounded-lg border border-slate-200 p-3">
													<p className="text-xs text-slate-500 capitalize">
														{tier.durationType}
													</p>
													<p className="text-lg font-bold text-slate-800 mt-1">
														${tier.pricePerDay}
														<span className="text-xs font-normal text-slate-400">
															/day
														</span>
													</p>
													<p className="text-xs text-slate-400">
														${tier.totalAmount} total
													</p>
												</div>
											))}
										</div>
									</div>

									{/* Items */}
									<div>
										<p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-2">
											What's Included
										</p>
										<div className="space-y-1">
											{planDetail.items
												.sort((a, b) => a.displayOrder - b.displayOrder)
												.map((item) => (
													<div
														key={item.id}
														className="flex items-center gap-2 text-sm">
														<Check
															size={14}
															className="text-green-500 flex-shrink-0"
														/>
														<span className="text-slate-700">
															{item.quantity}× {item.menuItemName}
														</span>
														<Badge
															variant="secondary"
															className="text-xs bg-slate-100">
															{item.portionSize}
														</Badge>
													</div>
												))}
										</div>
									</div>

									{/* Skip settings */}
									<div className="flex items-center gap-6 text-xs text-slate-500">
										<span>
											Skip allowed:{" "}
											<span
												className={
													planDetail.allowSkip
														? "text-green-600 font-medium"
														: "text-red-500 font-medium"
												}>
												{planDetail.allowSkip ? "Yes" : "No"}
											</span>
										</span>
										{planDetail.allowSkip && (
											<>
												<span>Notice: {planDetail.minSkipNoticeHours}hrs</span>
												<span>
													Max skips/cycle:{" "}
													{planDetail.maxSkipsPerCycle ?? "Unlimited"}
												</span>
											</>
										)}
										<span>
											Expiry reminder: {planDetail.expiryReminderDays} days
											before
										</span>
									</div>
								</div>
							)}
						</div>
					))}
				</div>
			)}

			{/* Create Plan Dialog */}
			<PlanDialog
				open={dialogOpen}
				onClose={() => setDialogOpen(false)}
				menuItems={menuItems}
				categories={categories}
				onSubmit={(data) => createPlan.mutate(data)}
				loading={createPlan.isPending}
			/>
		</div>
	);
}

// ── Plan Dialog ───────────────────────────────────────────────
function PlanDialog({
	open,
	onClose,
	menuItems,
	categories,
	onSubmit,
	loading,
}: {
	open: boolean;
	onClose: () => void;
	menuItems: MenuItem[];
	categories: Category[];
	onSubmit: (data: PlanForm) => void;
	loading: boolean;
}) {
	const [step, setStep] = useState<1 | 2 | 3>(1);

	const form = useForm<PlanForm>({
		resolver: zodResolver(planSchema),
		defaultValues: {
			name: "",
			description: "",
			dietaryType: "",
			boxType: "",
			allowSkip: false,
			expiryReminderDays: 3,
			pricingTiers: [
				{
					durationType: "daily",
					pricePerDay: 0,
					billingCycleDays: 1,
					totalAmount: 0,
				},
				{
					durationType: "weekly",
					pricePerDay: 0,
					billingCycleDays: 7,
					totalAmount: 0,
				},
				{
					durationType: "monthly",
					pricePerDay: 0,
					billingCycleDays: 30,
					totalAmount: 0,
				},
			],
			items: [],
		},
	});

	const { fields: tierFields } = useFieldArray({
		control: form.control,
		name: "pricingTiers",
	});

	const {
		fields: itemFields,
		append: appendItem,
		remove: removeItem,
	} = useFieldArray({
		control: form.control,
		name: "items",
	});

	// Auto-calculate totalAmount when pricePerDay changes
	const watchTiers = form.watch("pricingTiers");
	const updateTotal = (index: number, pricePerDay: number) => {
		const days = billingCycleDays[watchTiers[index]?.durationType] ?? 1;
		form.setValue(
			`pricingTiers.${index}.totalAmount`,
			parseFloat((pricePerDay * days).toFixed(2)),
		);
	};

	const handleClose = () => {
		form.reset();
		setStep(1);
		onClose();
	};

	const handleSubmit = form.handleSubmit((data) => {
		onSubmit(data);
	});

	// Get available portions for a selected menu item
	const getPortions = (menuItemId: string) =>
		menuItems.find((mi) => mi.id === menuItemId)?.availablePortions ?? [];

	return (
		<Dialog open={open} onOpenChange={handleClose}>
			<DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
				<DialogHeader>
					<DialogTitle>Create Tiffin Plan</DialogTitle>
				</DialogHeader>

				{/* Step Indicator */}
				<div className="flex items-center gap-2 py-2">
					{[
						{ n: 1, label: "Basic Info" },
						{ n: 2, label: "Pricing" },
						{ n: 3, label: "Items" },
					].map(({ n, label }) => (
						<div key={n} className="flex items-center gap-2">
							<button
								type="button"
								onClick={() => setStep(n as 1 | 2 | 3)}
								className={cn(
									"w-7 h-7 rounded-full text-xs font-semibold transition-colors",
									step === n
										? "bg-blue-600 text-white"
										: step > n
											? "bg-green-500 text-white"
											: "bg-slate-200 text-slate-500",
								)}>
								{step > n ? <Check size={12} className="mx-auto" /> : n}
							</button>
							<span
								className={cn(
									"text-sm",
									step === n ? "text-slate-800 font-medium" : "text-slate-400",
								)}>
								{label}
							</span>
							{n < 3 && <div className="w-8 h-px bg-slate-200" />}
						</div>
					))}
				</div>

				<form onSubmit={handleSubmit} className="space-y-4">
					{/* Step 1 — Basic Info */}
					{step === 1 && (
						<div className="space-y-4">
							<div className="space-y-2">
								<Label>Plan Name *</Label>
								<Input
									placeholder="e.g. Standard Tiffin, Premium Tiffin"
									{...form.register("name")}
								/>
								{form.formState.errors.name && (
									<p className="text-xs text-red-500">
										{form.formState.errors.name.message}
									</p>
								)}
							</div>

							<div className="space-y-2">
								<Label>Description</Label>
								<Input
									placeholder="Brief description shown to customers"
									{...form.register("description")}
								/>
							</div>

							<div className="grid grid-cols-2 gap-4">
								<div className="space-y-2">
									<Label>Dietary Type *</Label>
									<Select
										value={form.watch("dietaryType")}
										onValueChange={(v) => form.setValue("dietaryType", v)}>
										<SelectTrigger>
											<SelectValue placeholder="Select type" />
										</SelectTrigger>
										<SelectContent>
											<SelectItem value="veg">🌱 Vegetarian</SelectItem>
											<SelectItem value="non_veg">🍖 Non-vegetarian</SelectItem>
											<SelectItem value="both">🔀 Both options</SelectItem>
										</SelectContent>
									</Select>
									{form.formState.errors.dietaryType && (
										<p className="text-xs text-red-500">
											{form.formState.errors.dietaryType.message}
										</p>
									)}
								</div>

								<div className="space-y-2">
									<Label>Box Type</Label>
									<Select
										value={form.watch("boxType") ?? ""}
										onValueChange={(v) => form.setValue("boxType", v)}>
										<SelectTrigger>
											<SelectValue placeholder="Select box" />
										</SelectTrigger>
										<SelectContent>
											<SelectItem value="2_compartment">
												2 Compartment
											</SelectItem>
											<SelectItem value="3_compartment">
												3 Compartment
											</SelectItem>
											<SelectItem value="4_compartment">
												4 Compartment
											</SelectItem>
										</SelectContent>
									</Select>
								</div>
							</div>

							<div className="border border-slate-200 rounded-lg p-4 space-y-3">
								<div className="flex items-center justify-between">
									<div>
										<p className="text-sm font-medium text-slate-800">
											Allow Skip
										</p>
										<p className="text-xs text-slate-400">
											Let customers skip a delivery day
										</p>
									</div>
									<button
										type="button"
										onClick={() =>
											form.setValue("allowSkip", !form.watch("allowSkip"))
										}
										className={cn(
											"w-10 h-6 rounded-full transition-colors relative",
											form.watch("allowSkip") ? "bg-blue-600" : "bg-slate-300",
										)}>
										<span
											className={cn(
												"absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform",
												form.watch("allowSkip")
													? "translate-x-4"
													: "translate-x-0.5",
											)}
										/>
									</button>
								</div>

								{form.watch("allowSkip") && (
									<div className="grid grid-cols-2 gap-3 pt-2 border-t border-slate-100">
										<div className="space-y-1">
											<Label className="text-xs">Min notice (hours)</Label>
											<Input
												type="number"
												placeholder="24"
												className="h-8 text-sm"
												{...form.register("minSkipNoticeHours")}
											/>
										</div>
										<div className="space-y-1">
											<Label className="text-xs">Max skips per cycle</Label>
											<Input
												type="number"
												placeholder="4"
												className="h-8 text-sm"
												{...form.register("maxSkipsPerCycle")}
											/>
										</div>
									</div>
								)}
							</div>

							<div className="space-y-2">
								<Label>Expiry Reminder (days before)</Label>
								<Input
									type="number"
									defaultValue={3}
									{...form.register("expiryReminderDays")}
								/>
								<p className="text-xs text-slate-400">
									Send renewal reminder this many days before plan expires
								</p>
							</div>

							<div className="flex justify-end">
								<Button
									type="button"
									className="bg-blue-600 hover:bg-blue-700"
									onClick={() => setStep(2)}>
									Next: Pricing →
								</Button>
							</div>
						</div>
					)}

					{/* Step 2 — Pricing Tiers */}
					{step === 2 && (
						<div className="space-y-4">
							<p className="text-sm text-slate-500">
								Set prices for each subscription duration. Total amount is
								calculated automatically.
							</p>

							{tierFields.map((field, index) => (
								<div
									key={field.id}
									className="border border-slate-200 rounded-lg p-4 space-y-3">
									<p className="text-sm font-medium text-slate-800 capitalize">
										{field.durationType} Subscription
									</p>
									<div className="grid grid-cols-2 gap-3">
										<div className="space-y-1">
											<Label className="text-xs">Price per day (CAD)</Label>
											<Input
												type="number"
												step="0.01"
												placeholder="0.00"
												className="h-8"
												{...form.register(`pricingTiers.${index}.pricePerDay`)}
												onChange={(e) => {
													form.setValue(
														`pricingTiers.${index}.pricePerDay`,
														parseFloat(e.target.value) || 0,
													);
													updateTotal(index, parseFloat(e.target.value) || 0);
												}}
											/>
										</div>
										<div className="space-y-1">
											<Label className="text-xs">
												Total amount ({field.billingCycleDays} days)
											</Label>
											<Input
												type="number"
												step="0.01"
												className="h-8 bg-slate-50"
												readOnly
												{...form.register(`pricingTiers.${index}.totalAmount`)}
											/>
										</div>
									</div>
									{form.formState.errors.pricingTiers?.[index] && (
										<p className="text-xs text-red-500">
											Please enter a valid price
										</p>
									)}
								</div>
							))}

							<div className="flex justify-between">
								<Button
									type="button"
									variant="outline"
									onClick={() => setStep(1)}>
									← Back
								</Button>
								<Button
									type="button"
									className="bg-blue-600 hover:bg-blue-700"
									onClick={() => setStep(3)}>
									Next: Items →
								</Button>
							</div>
						</div>
					)}

					{/* Step 3 — Plan Items */}
					{step === 3 && (
						<div className="space-y-4">
							<p className="text-sm text-slate-500">
								Add the food items included in this plan.
							</p>

							{/* Added Items */}
							{itemFields.length > 0 && (
								<div className="space-y-2">
									{itemFields.map((field, index) => {
										const selectedItem = menuItems.find(
											(mi) => mi.id === form.watch(`items.${index}.menuItemId`),
										);
										return (
											<div
												key={field.id}
												className="flex items-center gap-3 bg-slate-50 rounded-lg p-3">
												<div className="flex-1 grid grid-cols-4 gap-2">
													{/* Menu Item */}
													<div className="col-span-1">
														<Select
															value={form.watch(`items.${index}.menuItemId`)}
															onValueChange={(v) => {
																form.setValue(`items.${index}.menuItemId`, v);
																form.setValue(`items.${index}.portionSize`, "");
															}}>
															<SelectTrigger className="h-8 text-xs">
																<SelectValue placeholder="Item">
																	{menuItems.find(
																		(mi) =>
																			mi.id ===
																			form.watch(`items.${index}.menuItemId`),
																	)?.name ?? "Select item"}
																</SelectValue>
															</SelectTrigger>
															<SelectContent>
																{categories.map((cat) => (
																	<div key={cat.id}>
																		<p className="px-2 py-1 text-xs font-semibold text-slate-400 uppercase">
																			{cat.name}
																		</p>
																		{menuItems
																			.filter(
																				(mi) =>
																					mi.categoryId === cat.id &&
																					mi.isActive,
																			)
																			.map((mi) => (
																				<SelectItem key={mi.id} value={mi.id}>
																					{mi.name}
																				</SelectItem>
																			))}
																	</div>
																))}
															</SelectContent>
														</Select>
													</div>

													{/* Portion Size */}
													<div className="col-span-1">
														<Select
															value={form.watch(`items.${index}.portionSize`)}
															onValueChange={(v) =>
																form.setValue(`items.${index}.portionSize`, v)
															}
															disabled={!selectedItem}>
															<SelectTrigger className="h-8 text-xs">
																<SelectValue placeholder="Portion" />
															</SelectTrigger>
															<SelectContent>
																{getPortions(
																	form.watch(`items.${index}.menuItemId`),
																).map((p) => (
																	<SelectItem key={p} value={p}>
																		{p}
																	</SelectItem>
																))}
															</SelectContent>
														</Select>
													</div>

													{/* Quantity */}
													<div className="col-span-1">
														<Input
															type="number"
															step="0.5"
															placeholder="Qty"
															className="h-8 text-xs"
															{...form.register(`items.${index}.quantity`)}
														/>
													</div>

													{/* Display Order */}
													<div className="col-span-1">
														<Input
															type="number"
															placeholder="Order"
															className="h-8 text-xs"
															{...form.register(`items.${index}.displayOrder`)}
														/>
													</div>
												</div>

												<Button
													type="button"
													variant="ghost"
													size="icon"
													className="h-8 w-8 text-red-500 hover:bg-red-50 flex-shrink-0"
													onClick={() => removeItem(index)}>
													<Trash2 size={14} />
												</Button>
											</div>
										);
									})}
								</div>
							)}

							{/* Column Headers */}
							{itemFields.length > 0 && (
								<div className="grid grid-cols-4 gap-2 px-3 text-xs text-slate-400">
									<span>Item</span>
									<span>Portion</span>
									<span>Qty</span>
									<span>Order</span>
								</div>
							)}

							{/* Add Item Button */}
							<Button
								type="button"
								variant="outline"
								size="sm"
								className="w-full border-dashed"
								onClick={() =>
									appendItem({
										menuItemId: "",
										portionSize: "",
										quantity: 1,
										displayOrder: itemFields.length + 1,
									})
								}>
								<Plus size={14} className="mr-2" />
								Add Item to Plan
							</Button>

							{form.formState.errors.items && (
								<p className="text-xs text-red-500">
									{form.formState.errors.items.message}
								</p>
							)}

							<div className="flex justify-between pt-2">
								<Button
									type="button"
									variant="outline"
									onClick={() => setStep(2)}>
									← Back
								</Button>
								<Button
									type="submit"
									className="bg-blue-600 hover:bg-blue-700"
									disabled={loading}>
									{loading ? "Creating..." : "Create Plan"}
								</Button>
							</div>
						</div>
					)}
				</form>
			</DialogContent>
		</Dialog>
	);
}
