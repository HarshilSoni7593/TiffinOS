"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import api from "@/lib/api";
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
import { Plus, Trash2, Truck } from "lucide-react";
import { cn } from "@/lib/utils";

const schema = z.object({
	zoneId: z.string().optional(),
	pricingTierId: z.string().optional(),
	ruleType: z.string().min(1, "Rule type is required"),
	charge: z.coerce.number().min(0),
	freeAboveAmount: z.coerce.number().optional(),
	priority: z.coerce.number().min(1).default(1),
});

type ChargeForm = z.infer<typeof schema>;

const ruleTypeLabels: Record<string, string> = {
	global: "Global — applies to all zones and tiers",
	per_zone: "Per Zone — applies to a specific zone",
	per_tier: "Per Tier — applies to a specific pricing tier",
	zone_tier: "Zone + Tier — applies to a specific zone and tier combination",
};

export default function DeliveryChargesPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);

	const { data: rules = [], isLoading } = useQuery<any[]>({
		queryKey: ["deliveryChargeRules"],
		queryFn: () => api.get("/api/delivery-charges").then((r) => r.data),
	});

	const { data: zones = [] } = useQuery<any[]>({
		queryKey: ["zones"],
		queryFn: () => api.get("/api/zones").then((r) => r.data),
	});

	const { data: templates = [] } = useQuery<any[]>({
		queryKey: ["pricingTierTemplates"],
		queryFn: () => api.get("/api/setup/pricing-tiers").then((r) => r.data),
	});

	const form = useForm<ChargeForm>({
		resolver: zodResolver(schema),
		defaultValues: {
			ruleType: "",
			charge: 0,
			priority: 1,
		},
	});

	const watchRuleType = form.watch("ruleType");

	const createRule = useMutation({
		mutationFn: (data: ChargeForm) =>
			api.post("/api/delivery-charges", {
				zoneId: data.zoneId || null,
				pricingTierId: data.pricingTierId || null,
				chargeType: data.ruleType, // maps ruleType → chargeType
				amount: data.charge, // maps charge → amount
				minPlanPricePerDay: data.freeAboveAmount || null,
				priority: data.priority,
				description: null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["deliveryChargeRules"] });
			setDialogOpen(false);
			form.reset();
			toast.success("Delivery charge rule created");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create rule"),
	});

	const deleteRule = useMutation({
		mutationFn: (id: string) => api.delete(`/api/delivery-charges/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["deliveryChargeRules"] });
			toast.success("Rule deleted");
		},
		onError: () => toast.error("Failed to delete rule"),
	});

	return (
		<div className="space-y-6 max-w-4xl">
			{/* Explanation */}
			<div className="bg-blue-50 border border-blue-100 rounded-xl p-4">
				<p className="text-sm text-blue-800 font-medium">
					How delivery charge rules work
				</p>
				<p className="text-xs text-blue-600 mt-1">
					Rules are evaluated by priority — highest priority wins. A Zone + Tier
					rule overrides a Per Zone rule which overrides a Global rule. Set free
					delivery above a minimum order amount to incentivise longer
					subscriptions.
				</p>
			</div>

			{/* Actions */}
			<div className="flex justify-end">
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={() => {
						form.reset({ ruleType: "", charge: 0, priority: 1 });
						setDialogOpen(true);
					}}>
					<Plus size={16} className="mr-2" />
					Add Rule
				</Button>
			</div>

			{/* Rules List */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-16 w-full rounded-xl" />
					))}
				</div>
			) : rules.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<Truck size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						No delivery charge rules yet.
					</p>
					<p className="text-slate-400 text-xs mt-1">
						Add a global rule first, then override per zone or tier.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={() => setDialogOpen(true)}>
						<Plus size={16} className="mr-2" />
						Add First Rule
					</Button>
				</div>
			) : (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					<table className="w-full">
						<thead>
							<tr className="border-b border-slate-100 bg-slate-50">
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Rule Type
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Applies To
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Charge
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Free Above
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Priority
								</th>
								<th className="px-5 py-3" />
							</tr>
						</thead>
						<tbody>
							{rules
								.sort((a, b) => b.priority - a.priority)
								.map((rule) => (
									<tr
										key={rule.id}
										className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
										<td className="px-5 py-4">
											<Badge
												variant="secondary"
												className={cn(
													"text-xs",
													rule.chargeType === "flat"
														? "bg-slate-100 text-slate-600"
														: rule.chargeType === "free"
															? "bg-green-100 text-green-700"
															: "bg-blue-100 text-blue-700",
												)}>
												{rule.chargeType}
												{rule.pricingTierDuration &&
													` — ${rule.pricingTierDuration}`}
											</Badge>
										</td>
										<td className="px-5 py-4 text-sm text-slate-600">
											{rule.zoneName && (
												<span className="mr-2">📍 {rule.zoneName}</span>
											)}
											{rule.pricingTierDuration && (
												<span>⏱ {rule.pricingTierDuration}</span>
											)}
											{!rule.zoneName && !rule.pricingTierDuration && (
												<span className="text-slate-400">
													All zones & tiers
												</span>
											)}
											{rule.description && (
												<p className="text-xs text-slate-400 mt-0.5">
													{rule.description}
												</p>
											)}
										</td>
										<td className="px-5 py-4">
											{rule.chargeType === "free" ? (
												<span className="text-green-600 font-semibold text-sm">
													Free
												</span>
											) : (
												<span className="text-sm font-semibold text-slate-800">
													${(rule.amount ?? 0).toFixed(2)}
												</span>
											)}
										</td>
										<td className="px-5 py-4 text-sm text-slate-600">
											{rule.minPlanPricePerDay ? (
												<span className="text-green-600 font-medium">
													Above ${rule.minPlanPricePerDay}/day
												</span>
											) : (
												<span className="text-slate-400">—</span>
											)}
										</td>
										<td className="px-5 py-4">
											<span className="text-sm font-medium text-slate-700">
												{rule.priority}
											</span>
										</td>
										<td className="px-5 py-4 text-right">
											<Button
												variant="ghost"
												size="icon"
												className="h-8 w-8 text-red-500 hover:bg-red-50"
												onClick={() => {
													if (confirm("Delete this rule?"))
														deleteRule.mutate(rule.id);
												}}>
												<Trash2 size={14} />
											</Button>
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
						<DialogTitle>Add Delivery Charge Rule</DialogTitle>
					</DialogHeader>
					<form
						onSubmit={form.handleSubmit((d) => createRule.mutate(d))}
						className="space-y-4 pt-2">
						<div className="space-y-2">
							<Label>Rule Type *</Label>
							<Select
								value={form.watch("ruleType")}
								onValueChange={(v) => {
									form.setValue("ruleType", v);
									form.setValue("zoneId", "");
									form.setValue("pricingTierId", "");
								}}>
								<SelectTrigger>
									<SelectValue placeholder="Select rule type" />
								</SelectTrigger>
								<SelectContent>
									{Object.entries(ruleTypeLabels).map(([v, l]) => (
										<SelectItem key={v} value={v}>
											{l}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>

						{/* Zone selector */}
						{(watchRuleType === "per_zone" ||
							watchRuleType === "zone_tier") && (
							<div className="space-y-2">
								<Label>Zone *</Label>
								<Select
									value={form.watch("zoneId") ?? ""}
									onValueChange={(v) => form.setValue("zoneId", v)}>
									<SelectTrigger>
										<SelectValue placeholder="Select zone" />
									</SelectTrigger>
									<SelectContent>
										{zones.map((z: any) => (
											<SelectItem key={z.id} value={z.id}>
												{z.name}
											</SelectItem>
										))}
									</SelectContent>
								</Select>
							</div>
						)}

						{/* Tier selector */}
						{(watchRuleType === "per_tier" ||
							watchRuleType === "zone_tier") && (
							<div className="space-y-2">
								<Label>Pricing Tier *</Label>
								<Select
									value={form.watch("pricingTierId") ?? ""}
									onValueChange={(v) => form.setValue("pricingTierId", v)}>
									<SelectTrigger>
										<SelectValue placeholder="Select tier" />
									</SelectTrigger>
									<SelectContent>
										{templates.map((t: any) => (
											<SelectItem key={t.id} value={t.id}>
												{t.name} ({t.deliveryDays} deliveries)
											</SelectItem>
										))}
									</SelectContent>
								</Select>
							</div>
						)}

						<div className="grid grid-cols-2 gap-4">
							<div className="space-y-2">
								<Label>Delivery Charge (CAD) *</Label>
								<Input
									type="number"
									step="0.01"
									placeholder="0.00"
									{...form.register("charge")}
								/>
								<p className="text-xs text-slate-400">
									Set to 0 for free delivery
								</p>
							</div>
							<div className="space-y-2">
								<Label>Free Above Amount</Label>
								<Input
									type="number"
									step="0.01"
									placeholder="Optional"
									{...form.register("freeAboveAmount")}
								/>
								<p className="text-xs text-slate-400">
									Free if plan total exceeds this
								</p>
							</div>
						</div>

						<div className="space-y-2">
							<Label>Priority</Label>
							<Input type="number" min="1" {...form.register("priority")} />
							<p className="text-xs text-slate-400">
								Higher number = higher priority. Zone+Tier rules should have
								highest priority.
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
								disabled={createRule.isPending}>
								{createRule.isPending ? "Creating..." : "Create Rule"}
							</Button>
						</div>
					</form>
				</DialogContent>
			</Dialog>
		</div>
	);
}
