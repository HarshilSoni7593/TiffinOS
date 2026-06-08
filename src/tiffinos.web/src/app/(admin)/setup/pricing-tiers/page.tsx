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
import { toast } from "sonner";
import { Plus, Pencil, Trash2, Layers } from "lucide-react";
import { cn } from "@/lib/utils";

const schema = z.object({
	name: z.string().min(1, "Name is required"),
	durationKey: z.string().min(1, "Duration key is required"),
	deliveryDays: z.coerce.number().min(1, "Must be at least 1"),
	description: z.string().optional(),
});

type TierForm = z.infer<typeof schema>;

export default function PricingTiersPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);
	const [editing, setEditing] = useState<any>(null);

	const { data: templates = [], isLoading } = useQuery<any[]>({
		queryKey: ["pricingTierTemplates"],
		queryFn: () => api.get("/api/setup/pricing-tiers").then((r) => r.data),
	});

	const form = useForm<TierForm>({
		resolver: zodResolver(schema),
		defaultValues: {
			name: "",
			durationKey: "",
			deliveryDays: 1,
			description: "",
		},
	});

	const createTemplate = useMutation({
		mutationFn: (data: TierForm) =>
			api.post("/api/setup/pricing-tiers", {
				...data,
				description: data.description || null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["pricingTierTemplates"] });
			setDialogOpen(false);
			form.reset();
			toast.success("Pricing tier created");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create tier"),
	});

	const updateTemplate = useMutation({
		mutationFn: (data: TierForm & { id: string }) =>
			api.put(`/api/setup/pricing-tiers/${data.id}`, {
				name: data.name,
				deliveryDays: data.deliveryDays,
				description: data.description || null,
				isActive: true,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["pricingTierTemplates"] });
			setDialogOpen(false);
			setEditing(null);
			form.reset();
			toast.success("Pricing tier updated");
		},
		onError: () => toast.error("Failed to update tier"),
	});

	const deleteTemplate = useMutation({
		mutationFn: (id: string) => api.delete(`/api/setup/pricing-tiers/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["pricingTierTemplates"] });
			toast.success("Pricing tier deactivated");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Cannot deactivate this tier"),
	});

	const openCreate = () => {
		setEditing(null);
		form.reset({
			name: "",
			durationKey: "",
			deliveryDays: 1,
			description: "",
		});
		setDialogOpen(true);
	};

	const openEdit = (t: any) => {
		setEditing(t);
		form.reset({
			name: t.name,
			durationKey: t.durationKey,
			deliveryDays: t.deliveryDays,
			description: t.description ?? "",
		});
		setDialogOpen(true);
	};

	const onSubmit = (data: TierForm) => {
		if (editing) updateTemplate.mutate({ ...data, id: editing.id });
		else createTemplate.mutate(data);
	};

	return (
		<div className="space-y-6 max-w-3xl">
			{/* Explanation */}
			<div className="bg-blue-50 border border-blue-100 rounded-xl p-4">
				<p className="text-sm text-blue-800 font-medium">
					What are pricing tier templates?
				</p>
				<p className="text-xs text-blue-600 mt-1">
					Define your billing cycles here once — Daily, Weekly, Bi-weekly,
					Monthly, or any custom cycle. When creating a tiffin plan, you select
					from these templates and set the price for each. This keeps your
					billing structure consistent across all plans.
				</p>
			</div>

			{/* Actions */}
			<div className="flex justify-end">
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={openCreate}>
					<Plus size={16} className="mr-2" />
					Add Tier Template
				</Button>
			</div>

			{/* Templates List */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 4 }).map((_, i) => (
						<Skeleton key={i} className="h-16 w-full rounded-xl" />
					))}
				</div>
			) : templates.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<Layers size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						No pricing tier templates yet.
					</p>
					<p className="text-slate-400 text-xs mt-1">
						Add at least one before creating tiffin plans.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={openCreate}>
						<Plus size={16} className="mr-2" />
						Add First Template
					</Button>
				</div>
			) : (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					<table className="w-full">
						<thead>
							<tr className="border-b border-slate-100 bg-slate-50">
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Name
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Duration Key
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Deliveries
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Description
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Plans Using
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Status
								</th>
								<th className="px-5 py-3" />
							</tr>
						</thead>
						<tbody>
							{templates.map((t) => (
								<tr
									key={t.id}
									className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
									<td className="px-5 py-4">
										<p className="text-sm font-medium text-slate-800">
											{t.name}
										</p>
									</td>
									<td className="px-5 py-4">
										<code className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">
											{t.durationKey}
										</code>
									</td>
									<td className="px-5 py-4">
										<span className="text-sm font-semibold text-blue-600">
											{t.deliveryDays}
										</span>
										<span className="text-xs text-slate-400 ml-1">
											deliveries
										</span>
									</td>
									<td className="px-5 py-4 text-sm text-slate-500">
										{t.description ?? "—"}
									</td>
									<td className="px-5 py-4">
										<span
											className={cn(
												"text-sm font-medium",
												t.plansUsingThis > 0
													? "text-blue-600"
													: "text-slate-400",
											)}>
											{t.plansUsingThis} plan
											{t.plansUsingThis !== 1 ? "s" : ""}
										</span>
									</td>
									<td className="px-5 py-4">
										<Badge
											variant="secondary"
											className={cn(
												"text-xs",
												t.isActive
													? "bg-green-100 text-green-700"
													: "bg-slate-100 text-slate-500",
											)}>
											{t.isActive ? "Active" : "Inactive"}
										</Badge>
									</td>
									<td className="px-5 py-4">
										<div className="flex items-center justify-end gap-2">
											<Button
												variant="ghost"
												size="icon"
												className="h-8 w-8"
												onClick={() => openEdit(t)}>
												<Pencil size={14} />
											</Button>
											{t.plansUsingThis === 0 && t.isActive && (
												<Button
													variant="ghost"
													size="icon"
													className="h-8 w-8 text-red-500 hover:bg-red-50"
													onClick={() => {
														if (confirm(`Deactivate "${t.name}"?`))
															deleteTemplate.mutate(t.id);
													}}>
													<Trash2 size={14} />
												</Button>
											)}
										</div>
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
						<DialogTitle>
							{editing ? "Edit Tier Template" : "Add Tier Template"}
						</DialogTitle>
					</DialogHeader>
					<form
						onSubmit={form.handleSubmit(onSubmit)}
						className="space-y-4 pt-2">
						<div className="space-y-2">
							<Label>Display Name *</Label>
							<Input
								placeholder="e.g. Monthly, Bi-weekly"
								{...form.register("name")}
							/>
							{form.formState.errors.name && (
								<p className="text-xs text-red-500">
									{form.formState.errors.name.message}
								</p>
							)}
						</div>

						<div className="space-y-2">
							<Label>Duration Key *</Label>
							<Input
								placeholder="e.g. monthly, bi_weekly"
								disabled={!!editing}
								{...form.register("durationKey")}
							/>
							<p className="text-xs text-slate-400">
								Lowercase, use underscore for spaces. Cannot be changed after
								creation.
							</p>
							{form.formState.errors.durationKey && (
								<p className="text-xs text-red-500">
									{form.formState.errors.durationKey.message}
								</p>
							)}
						</div>

						<div className="space-y-2">
							<Label>Number of Deliveries *</Label>
							<Input
								type="number"
								min="1"
								placeholder="e.g. 20"
								{...form.register("deliveryDays")}
							/>
							<p className="text-xs text-slate-400">
								Total deliveries included in one billing cycle
							</p>
							{form.formState.errors.deliveryDays && (
								<p className="text-xs text-red-500">
									{form.formState.errors.deliveryDays.message}
								</p>
							)}
						</div>

						<div className="space-y-2">
							<Label>Description</Label>
							<Input
								placeholder="e.g. 5 days a week, 4 weeks"
								{...form.register("description")}
							/>
						</div>

						<div className="flex justify-end gap-2 pt-2">
							<Button
								type="button"
								variant="outline"
								onClick={() => {
									setDialogOpen(false);
									setEditing(null);
									form.reset();
								}}>
								Cancel
							</Button>
							<Button
								type="submit"
								className="bg-blue-600 hover:bg-blue-700"
								disabled={createTemplate.isPending || updateTemplate.isPending}>
								{createTemplate.isPending || updateTemplate.isPending
									? "Saving..."
									: editing
										? "Save Changes"
										: "Create Template"}
							</Button>
						</div>
					</form>
				</DialogContent>
			</Dialog>
		</div>
	);
}
