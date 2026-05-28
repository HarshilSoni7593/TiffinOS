"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { Zone } from "@/types/api";
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
import { toast } from "sonner";
import { Plus, Pencil, Trash2, MapPin } from "lucide-react";
import { useForm, useFieldArray } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

// ── Schema ────────────────────────────────────────────────────
const coordSchema = z.object({
	lat: z.coerce.number().min(-90).max(90),
	lng: z.coerce.number().min(-180).max(180),
});

const zoneSchema = z.object({
	name: z.string().min(1, "Name is required"),
	zoneCode: z.string().min(1, "Zone code is required"),
	colorHex: z.string().optional(),
	polygonCoords: z
		.array(coordSchema)
		.min(3, "At least 3 coordinates required to form a valid zone boundary"),
});

type ZoneForm = z.infer<typeof zoneSchema>;

export default function ZonesPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);
	const [editingZone, setEditingZone] = useState<Zone | null>(null);

	const { data: zones = [], isLoading } = useQuery<Zone[]>({
		queryKey: ["zones"],
		queryFn: () => api.get("/api/zones").then((r) => r.data),
	});

	const createZone = useMutation({
		mutationFn: (data: ZoneForm) => api.post("/api/zones", data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["zones"] });
			setDialogOpen(false);
			toast.success("Zone created successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to create zone"),
	});

	const deactivateZone = useMutation({
		mutationFn: (id: string) => api.delete(`/api/zones/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["zones"] });
			toast.success("Zone deactivated");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to deactivate zone"),
	});

	const openCreate = () => {
		setEditingZone(null);
		setDialogOpen(true);
	};

	return (
		<div className="space-y-6">
			{/* Actions */}
			<div className="flex justify-end">
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={openCreate}>
					<Plus size={16} className="mr-2" />
					Add Zone
				</Button>
			</div>

			{/* Zones List */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-20 w-full rounded-xl" />
					))}
				</div>
			) : zones.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<MapPin size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						No delivery zones yet. Add your first zone.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={openCreate}>
						<Plus size={16} className="mr-2" />
						Add First Zone
					</Button>
				</div>
			) : (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					<table className="w-full">
						<thead>
							<tr className="border-b border-slate-100 bg-slate-50">
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Zone
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Code
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Active Subscriptions
								</th>
								<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
									Status
								</th>
								<th className="px-5 py-3" />
							</tr>
						</thead>
						<tbody>
							{zones.map((zone) => (
								<tr
									key={zone.id}
									className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
									<td className="px-5 py-4">
										<div className="flex items-center gap-3">
											{zone.colorHex && (
												<div
													className="w-3 h-3 rounded-full flex-shrink-0"
													style={{ backgroundColor: zone.colorHex }}
												/>
											)}
											<span className="text-sm font-medium text-slate-800">
												{zone.name}
											</span>
										</div>
									</td>
									<td className="px-5 py-4">
										<code className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded">
											{zone.zoneCode}
										</code>
									</td>
									<td className="px-5 py-4 text-sm text-slate-600">
										{zone.activeSubscriptionCount}
									</td>
									<td className="px-5 py-4">
										<Badge
											variant="secondary"
											className="bg-green-100 text-green-700 hover:bg-green-100 text-xs">
											Active
										</Badge>
									</td>
									<td className="px-5 py-4 text-right">
										<div className="flex items-center justify-end gap-2">
											{zone.activeSubscriptionCount === 0 && (
												<Button
													variant="ghost"
													size="icon"
													className="h-8 w-8 text-red-500 hover:text-red-600 hover:bg-red-50"
													onClick={() => {
														if (confirm(`Deactivate zone "${zone.name}"?`))
															deactivateZone.mutate(zone.id);
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

			{/* Zone Dialog */}
			<ZoneDialog
				open={dialogOpen}
				onClose={() => {
					setDialogOpen(false);
					setEditingZone(null);
				}}
				editing={editingZone}
				onSubmit={(data) => createZone.mutate(data)}
				loading={createZone.isPending}
			/>
		</div>
	);
}

// ── Zone Dialog ───────────────────────────────────────────────
function ZoneDialog({
	open,
	onClose,
	editing,
	onSubmit,
	loading,
}: {
	open: boolean;
	onClose: () => void;
	editing: Zone | null;
	onSubmit: (data: ZoneForm) => void;
	loading: boolean;
}) {
	const form = useForm<ZoneForm>({
		resolver: zodResolver(zoneSchema),
		defaultValues: {
			name: "",
			zoneCode: "",
			colorHex: "#3B82F6",
			polygonCoords: [
				{ lat: 0, lng: 0 },
				{ lat: 0, lng: 0 },
				{ lat: 0, lng: 0 },
			],
		},
	});

	const { fields, append, remove } = useFieldArray({
		control: form.control,
		name: "polygonCoords",
	});

	return (
		<Dialog open={open} onOpenChange={onClose}>
			<DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
				<DialogHeader>
					<DialogTitle>
						{editing ? "Edit Zone" : "Add Delivery Zone"}
					</DialogTitle>
				</DialogHeader>

				<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 pt-2">
					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Zone Name *</Label>
							<Input
								placeholder="e.g. Downtown Toronto"
								{...form.register("name")}
							/>
							{form.formState.errors.name && (
								<p className="text-xs text-red-500">
									{form.formState.errors.name.message}
								</p>
							)}
						</div>
						<div className="space-y-2">
							<Label>Zone Code *</Label>
							<Input placeholder="e.g. ZONE-A" {...form.register("zoneCode")} />
							{form.formState.errors.zoneCode && (
								<p className="text-xs text-red-500">
									{form.formState.errors.zoneCode.message}
								</p>
							)}
						</div>
					</div>

					<div className="space-y-2">
						<Label>Color (for map display)</Label>
						<div className="flex items-center gap-3">
							<input
								type="color"
								className="h-9 w-16 rounded border border-slate-200 cursor-pointer"
								{...form.register("colorHex")}
							/>
							<Input
								placeholder="#3B82F6"
								className="flex-1"
								{...form.register("colorHex")}
							/>
						</div>
					</div>

					{/* Polygon Coordinates */}
					<div className="space-y-2">
						<div className="flex items-center justify-between">
							<Label>Zone Boundary Coordinates *</Label>
							<Button
								type="button"
								variant="outline"
								size="sm"
								onClick={() => append({ lat: 0, lng: 0 })}>
								<Plus size={12} className="mr-1" />
								Add Point
							</Button>
						</div>
						<p className="text-xs text-slate-400">
							Enter the lat/lng coordinates that form the zone boundary. Minimum
							3 points required. Get coordinates from Google Maps by
							right-clicking any location.
						</p>

						<div className="space-y-2 max-h-48 overflow-y-auto">
							{fields.map((field, index) => (
								<div key={field.id} className="flex items-center gap-2">
									<span className="text-xs text-slate-400 w-6 flex-shrink-0">
										{index + 1}.
									</span>
									<Input
										type="number"
										step="0.0001"
										placeholder="Latitude"
										className="h-8 text-xs"
										{...form.register(`polygonCoords.${index}.lat`)}
									/>
									<Input
										type="number"
										step="0.0001"
										placeholder="Longitude"
										className="h-8 text-xs"
										{...form.register(`polygonCoords.${index}.lng`)}
									/>
									{fields.length > 3 && (
										<Button
											type="button"
											variant="ghost"
											size="icon"
											className="h-8 w-8 text-red-500 flex-shrink-0"
											onClick={() => remove(index)}>
											<Trash2 size={12} />
										</Button>
									)}
								</div>
							))}
						</div>

						{form.formState.errors.polygonCoords && (
							<p className="text-xs text-red-500">
								{form.formState.errors.polygonCoords.message}
							</p>
						)}
					</div>

					<div className="flex justify-end gap-2 pt-2">
						<Button type="button" variant="outline" onClick={onClose}>
							Cancel
						</Button>
						<Button
							type="submit"
							className="bg-blue-600 hover:bg-blue-700"
							disabled={loading}>
							{loading ? "Saving..." : editing ? "Save Changes" : "Create Zone"}
						</Button>
					</div>
				</form>
			</DialogContent>
		</Dialog>
	);
}
