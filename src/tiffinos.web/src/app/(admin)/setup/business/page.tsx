"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import api from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";

const schema = z.object({
	timezone: z.string().min(1),
	currency: z.string().min(1),
	prepScheduleTime: z.string().min(1),
	prepScheduleOffsetDays: z.coerce.number().min(1),
	dispatchScheduleTime: z.string().min(1),
	deliveryStartTime: z.string().min(1),
	deliveryEndTime: z.string().min(1),
	smsSenderName: z.string().optional(),
});

type SettingsForm = z.infer<typeof schema>;

const TIMEZONES = [
	"America/Toronto",
	"America/Vancouver",
	"America/Edmonton",
	"America/Winnipeg",
	"America/Halifax",
	"America/Regina",
	"America/St_Johns",
];

const CURRENCIES = ["CAD", "USD", "INR", "GBP", "EUR"];

export default function BusinessSettingsPage() {
	const queryClient = useQueryClient();

	const { data: settings, isLoading } = useQuery<any>({
		queryKey: ["businessSettings"],
		queryFn: () => api.get("/api/setup/business").then((r) => r.data),
	});

	const form = useForm<SettingsForm>({
		resolver: zodResolver(schema),
		defaultValues: {
			timezone: "America/Toronto",
			currency: "CAD",
			prepScheduleTime: "21:00:00",
			prepScheduleOffsetDays: 1,
			dispatchScheduleTime: "07:00:00",
			deliveryStartTime: "08:00:00",
			deliveryEndTime: "20:00:00",
			smsSenderName: "",
		},
	});

	// Populate form when data loads
	useEffect(() => {
		if (settings) {
			form.reset({
				timezone: settings.timezone,
				currency: settings.currency,
				prepScheduleTime: settings.prepScheduleTime,
				prepScheduleOffsetDays: settings.prepScheduleOffsetDays,
				dispatchScheduleTime: settings.dispatchScheduleTime,
				deliveryStartTime: settings.deliveryStartTime,
				deliveryEndTime: settings.deliveryEndTime,
				smsSenderName: settings.smsSenderName ?? "",
			});
		}
	}, [settings, form]);

	const update = useMutation({
		mutationFn: (data: SettingsForm) =>
			api.put("/api/setup/business", {
				...data,
				smsSenderName: data.smsSenderName || null,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["businessSettings"] });
			toast.success("Business settings updated successfully");
		},
		onError: () => toast.error("Failed to update settings"),
	});

	if (isLoading)
		return (
			<div className="space-y-4 max-w-2xl">
				{Array.from({ length: 6 }).map((_, i) => (
					<Skeleton key={i} className="h-12 w-full rounded-xl" />
				))}
			</div>
		);

	return (
		<div className="max-w-2xl space-y-6">
			{/* Restaurant Info — read only */}
			<div className="bg-white rounded-xl border border-slate-200 p-6">
				<h2 className="font-semibold text-slate-800 mb-4">Restaurant Info</h2>
				<div className="grid grid-cols-2 gap-4 text-sm">
					<div>
						<p className="text-slate-400 text-xs">Restaurant Name</p>
						<p className="font-medium text-slate-800 mt-1">{settings?.name}</p>
					</div>
					<div>
						<p className="text-slate-400 text-xs">URL Slug</p>
						<code className="text-xs bg-slate-100 text-slate-600 px-2 py-1 rounded mt-1 block">
							{settings?.slug}
						</code>
					</div>
					<div>
						<p className="text-slate-400 text-xs">Plan Tier</p>
						<p className="font-medium text-slate-800 mt-1 capitalize">
							{settings?.planTier}
						</p>
					</div>
				</div>
			</div>

			{/* Editable Settings */}
			<form
				onSubmit={form.handleSubmit((d) => update.mutate(d))}
				className="space-y-4">
				{/* Localisation */}
				<div className="bg-white rounded-xl border border-slate-200 p-6 space-y-4">
					<h2 className="font-semibold text-slate-800">Localisation</h2>

					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Timezone</Label>
							<Select
								value={form.watch("timezone")}
								onValueChange={(v) => form.setValue("timezone", v)}>
								<SelectTrigger>
									<SelectValue />
								</SelectTrigger>
								<SelectContent>
									{TIMEZONES.map((tz) => (
										<SelectItem key={tz} value={tz}>
											{tz}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
						<div className="space-y-2">
							<Label>Currency</Label>
							<Select
								value={form.watch("currency")}
								onValueChange={(v) => form.setValue("currency", v)}>
								<SelectTrigger>
									<SelectValue />
								</SelectTrigger>
								<SelectContent>
									{CURRENCIES.map((c) => (
										<SelectItem key={c} value={c}>
											{c}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
					</div>
				</div>

				{/* Kitchen Schedule */}
				<div className="bg-white rounded-xl border border-slate-200 p-6 space-y-4">
					<div>
						<h2 className="font-semibold text-slate-800">Kitchen Schedule</h2>
						<p className="text-xs text-slate-400 mt-1">
							Controls when the daily packing summary and driver dispatch list
							are generated automatically.
						</p>
					</div>

					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Packing Summary Time</Label>
							<Input type="time" {...form.register("prepScheduleTime")} />
							<p className="text-xs text-slate-400">
								When cook gets tomorrow's prep list
							</p>
						</div>
						<div className="space-y-2">
							<Label>Days Before Delivery</Label>
							<Input
								type="number"
								min="1"
								{...form.register("prepScheduleOffsetDays")}
							/>
							<p className="text-xs text-slate-400">Usually 1 (night before)</p>
						</div>
						<div className="space-y-2">
							<Label>Dispatch List Time</Label>
							<Input type="time" {...form.register("dispatchScheduleTime")} />
							<p className="text-xs text-slate-400">
								When drivers get today's route
							</p>
						</div>
					</div>
				</div>

				{/* Delivery Hours */}
				<div className="bg-white rounded-xl border border-slate-200 p-6 space-y-4">
					<h2 className="font-semibold text-slate-800">Delivery Hours</h2>
					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Delivery Start Time</Label>
							<Input type="time" {...form.register("deliveryStartTime")} />
						</div>
						<div className="space-y-2">
							<Label>Delivery End Time</Label>
							<Input type="time" {...form.register("deliveryEndTime")} />
						</div>
					</div>
				</div>

				{/* Notifications */}
				<div className="bg-white rounded-xl border border-slate-200 p-6 space-y-4">
					<h2 className="font-semibold text-slate-800">Notifications</h2>
					<div className="space-y-2">
						<Label>SMS Sender Name</Label>
						<Input
							placeholder="e.g. SpiceKitchen"
							maxLength={20}
							{...form.register("smsSenderName")}
						/>
						<p className="text-xs text-slate-400">
							Max 20 characters. Shown as sender name on SMS alerts.
						</p>
					</div>
				</div>

				<Button
					type="submit"
					className="bg-blue-600 hover:bg-blue-700 w-full"
					disabled={update.isPending}>
					{update.isPending ? "Saving..." : "Save Settings"}
				</Button>
			</form>
		</div>
	);
}
