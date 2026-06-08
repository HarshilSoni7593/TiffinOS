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
import { Plus, Trash2, CalendarOff } from "lucide-react";
import { cn } from "@/lib/utils";

const schema = z.object({
	holidayDate: z.string().min(1, "Date is required"),
	reason: z.string().optional(),
	isDeliveryOff: z.boolean().default(true),
});

type HolidayForm = z.infer<typeof schema>;

export default function HolidaysPage() {
	const queryClient = useQueryClient();
	const [dialogOpen, setDialogOpen] = useState(false);
	const currentYear = new Date().getFullYear();
	const [year, setYear] = useState(currentYear);

	const { data: holidays = [], isLoading } = useQuery<any[]>({
		queryKey: ["holidays", year],
		queryFn: () =>
			api
				.get("/api/setup/holidays", {
					params: { year },
				})
				.then((r) => r.data),
	});

	const form = useForm<HolidayForm>({
		resolver: zodResolver(schema),
		defaultValues: {
			holidayDate: "",
			reason: "",
			isDeliveryOff: true,
		},
	});

	const createHoliday = useMutation({
		mutationFn: (data: HolidayForm) =>
			api.post("/api/setup/holidays", {
				holidayDate: data.holidayDate,
				reason: data.reason || null,
				isDeliveryOff: data.isDeliveryOff,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["holidays"] });
			setDialogOpen(false);
			form.reset();
			toast.success("Holiday added");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to add holiday"),
	});

	const deleteHoliday = useMutation({
		mutationFn: (id: string) => api.delete(`/api/setup/holidays/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["holidays"] });
			toast.success("Holiday removed");
		},
		onError: () => toast.error("Failed to remove holiday"),
	});

	// Group holidays by month
	const grouped = holidays.reduce(
		(acc, h) => {
			const month = new Date(h.holidayDate + "T00:00:00").toLocaleString(
				"default",
				{ month: "long" },
			);
			if (!acc[month]) acc[month] = [];
			acc[month].push(h);
			return acc;
		},
		{} as Record<string, any[]>,
	);

	return (
		<div className="space-y-6 max-w-3xl">
			{/* Explanation */}
			<div className="bg-amber-50 border border-amber-100 rounded-xl p-4">
				<p className="text-sm text-amber-800 font-medium">How holidays work</p>
				<p className="text-xs text-amber-700 mt-1">
					On days marked as delivery off, the packing summary and dispatch list
					will not generate. Subscriptions are not charged for skipped holidays.
				</p>
			</div>

			{/* Year selector + Add button */}
			<div className="flex items-center justify-between">
				<div className="flex items-center gap-2">
					<Button
						variant="outline"
						size="sm"
						onClick={() => setYear((y) => y - 1)}>
						← {year - 1}
					</Button>
					<span className="text-sm font-semibold text-slate-700 px-3">
						{year}
					</span>
					<Button
						variant="outline"
						size="sm"
						onClick={() => setYear((y) => y + 1)}>
						{year + 1} →
					</Button>
				</div>
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={() => {
						form.reset({
							holidayDate: "",
							reason: "",
							isDeliveryOff: true,
						});
						setDialogOpen(true);
					}}>
					<Plus size={16} className="mr-2" />
					Add Holiday
				</Button>
			</div>

			{/* Holidays List */}
			{isLoading ? (
				<div className="space-y-3">
					{Array.from({ length: 3 }).map((_, i) => (
						<Skeleton key={i} className="h-16 w-full rounded-xl" />
					))}
				</div>
			) : holidays.length === 0 ? (
				<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
					<CalendarOff size={32} className="text-slate-300 mx-auto mb-3" />
					<p className="text-slate-400 text-sm">
						No holidays added for {year}.
					</p>
					<Button
						size="sm"
						className="mt-4 bg-blue-600 hover:bg-blue-700"
						onClick={() => setDialogOpen(true)}>
						<Plus size={16} className="mr-2" />
						Add Holiday
					</Button>
				</div>
			) : (
				<div className="space-y-4">
					{Object.entries(grouped).map(([month, items]) => (
						<div
							key={month}
							className="bg-white rounded-xl border border-slate-200 overflow-hidden">
							{/* Month header */}
							<div className="px-5 py-3 bg-slate-50 border-b border-slate-100">
								<p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">
									{month} {year}
								</p>
							</div>

							{/* Days */}
							{(items as any[]).map((h) => (
								<div
									key={h.id}
									className="flex items-center justify-between px-5 py-3 border-b border-slate-50 last:border-0 hover:bg-slate-50">
									<div className="flex items-center gap-4">
										<div className="w-10 h-10 rounded-lg bg-red-50 flex items-center justify-center flex-shrink-0">
											<span className="text-sm font-bold text-red-600">
												{new Date(h.holidayDate + "T00:00:00").getDate()}
											</span>
										</div>
										<div>
											<p className="text-sm font-medium text-slate-800">
												{new Date(
													h.holidayDate + "T00:00:00",
												).toLocaleDateString("en-CA", {
													weekday: "long",
													month: "long",
													day: "numeric",
												})}
											</p>
											{h.reason && (
												<p className="text-xs text-slate-400 mt-0.5">
													{h.reason}
												</p>
											)}
										</div>
									</div>

									<div className="flex items-center gap-3">
										<Badge
											variant="secondary"
											className={cn(
												"text-xs",
												h.isDeliveryOff
													? "bg-red-100 text-red-700"
													: "bg-amber-100 text-amber-700",
											)}>
											{h.isDeliveryOff ? "No delivery" : "Reduced service"}
										</Badge>
										<Button
											variant="ghost"
											size="icon"
											className="h-8 w-8 text-red-500 hover:bg-red-50"
											onClick={() => {
												if (confirm("Remove this holiday?"))
													deleteHoliday.mutate(h.id);
											}}>
											<Trash2 size={14} />
										</Button>
									</div>
								</div>
							))}
						</div>
					))}
				</div>
			)}

			{/* Dialog */}
			<Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
				<DialogContent className="sm:max-w-md">
					<DialogHeader>
						<DialogTitle>Add Holiday</DialogTitle>
					</DialogHeader>
					<form
						onSubmit={form.handleSubmit((d) => createHoliday.mutate(d))}
						className="space-y-4 pt-2">
						<div className="space-y-2">
							<Label>Date *</Label>
							<input
								type="date"
								className="w-full h-9 px-3 rounded-lg border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
								{...form.register("holidayDate")}
							/>
							{form.formState.errors.holidayDate && (
								<p className="text-xs text-red-500">
									{form.formState.errors.holidayDate.message}
								</p>
							)}
						</div>

						<div className="space-y-2">
							<Label>Reason</Label>
							<Input
								placeholder="e.g. Diwali, Christmas, Staff holiday"
								{...form.register("reason")}
							/>
						</div>

						{/* Delivery off toggle */}
						<div className="flex items-center justify-between p-3 bg-slate-50 rounded-lg">
							<div>
								<p className="text-sm font-medium text-slate-800">
									Delivery off this day
								</p>
								<p className="text-xs text-slate-400">
									No packing summary or dispatch generated
								</p>
							</div>
							<button
								type="button"
								onClick={() =>
									form.setValue("isDeliveryOff", !form.watch("isDeliveryOff"))
								}
								className={cn(
									"w-10 h-6 rounded-full transition-colors relative flex-shrink-0",
									form.watch("isDeliveryOff") ? "bg-blue-600" : "bg-slate-300",
								)}>
								<span
									className={cn(
										"absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform",
										form.watch("isDeliveryOff")
											? "translate-x-4"
											: "translate-x-0.5",
									)}
								/>
							</button>
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
								disabled={createHoliday.isPending}>
								{createHoliday.isPending ? "Adding..." : "Add Holiday"}
							</Button>
						</div>
					</form>
				</DialogContent>
			</Dialog>
		</div>
	);
}
