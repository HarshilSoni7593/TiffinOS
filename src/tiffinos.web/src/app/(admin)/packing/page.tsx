"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { PackingSummary } from "@/types/api";
import { Skeleton } from "@/components/ui/skeleton";
import { RefreshCw, ChefHat } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function PackingSummaryPage() {
	const tomorrow = new Date();
	tomorrow.setDate(tomorrow.getDate() + 1);
	const defaultDate = tomorrow.toISOString().split("T")[0];
	const [selectedDate, setSelectedDate] = useState(defaultDate);
	const queryClient = useQueryClient();

	const {
		data: summary,
		isLoading,
		error,
	} = useQuery<PackingSummary>({
		queryKey: ["packingSummary", selectedDate],
		queryFn: () =>
			api
				.get(`/api/delivery-engine/packing-summary?date=${selectedDate}`)
				.then((r) => r.data),
	});

	const generate = useMutation({
		mutationFn: () => api.post("api/delivery-engine/generate-packing-summary"),
		onSuccess: () => {
			setTimeout(() => {
				queryClient.invalidateQueries({
					queryKey: ["packingSummary", selectedDate],
				});
			}, 2000);
		},
	});

	return (
		<div className="space-y-6">
			{/* Header bar */}
			<div className="flex items-center justify-between">
				<div className="flex items-center gap-3">
					<input
						type="date"
						value={selectedDate}
						onChange={(e) => setSelectedDate(e.target.value)}
						className="h-9 px-3 rounded-lg border border-slate-200 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
					/>
					{summary && (
						<span className="text-sm text-slate-500">
							{summary.totalBoxes} boxes to pack
						</span>
					)}
				</div>
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={() => generate.mutate()}
					disabled={generate.isPending}>
					<RefreshCw size={14} className="mr-2" />
					{generate.isPending ? "Generating..." : "Generate Summary"}
				</Button>
			</div>

			{/* content */}
			{isLoading ? <LoadingSkeleton /> : null}
			{!isLoading && (!summary || summary.items.length === 0) ? (
				<EmptyState />
			) : null}
			{!isLoading && summary && summary.items.length > 0 ? (
				<SummaryTable summary={summary} />
			) : null}
		</div>
	);
}

function LoadingSkeleton() {
	return (
		<div className="space-y-3">
			{Array.from({ length: 4 }).map((_, i) => (
				<Skeleton key={i} className="h-14 w-full rounded-xl" />
			))}
		</div>
	);
}

function EmptyState() {
	return (
		<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
			<ChefHat size={32} className="text-slate-300 mx-auto mb-3" />
			<p className="text-slate-400 text-sm">
				No packing summary for this date.
			</p>
			<p className="text-slate-400 text-xs mt-1">
				Click Generate Summary to create it.
			</p>
		</div>
	);
}

function SummaryTable({ summary }: { summary: PackingSummary }) {
	const grouped = summary.items.reduce(
		(acc, item) => {
			const key = item.category;
			if (!acc[key]) acc[key] = [];
			acc[key].push(item);
			return acc;
		},
		{} as Record<string, typeof summary.items>,
	);

	return (
		<div className="space-y-4">
			<div className="flex items-center gap-4 text-xs text-slate-400">
				<span>Date: {summary.date}</span>
				<span>
					Generated: {new Date(summary.generatedAt).toLocaleTimeString()}
				</span>
				<span className="font-semibold text-slate-600">
					Total boxes: {summary.totalBoxes}
				</span>
			</div>

			{/* Items grouped by categories */}
			{Object.entries(grouped).map(([category, items]) => (
				<div
					key={category}
					className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					{/* category header */}
					<div className="px-5 py-3 bg-slate-50 border-b border-slate-100">
						<p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">
							{category}
						</p>
					</div>

					{/* Items */}

					<table className="w-full">
						<thead>
							<tr className="border-b border-slate-100">
								<th className="text-left text-xs font-medium text-slate-400 px-5 py-2">
									Item
								</th>
								<th className="text-left text-xs font-medium text-slate-400 px-5 py-2">
									Portion Size
								</th>
								<th className="text-left text-xs font-medium text-slate-400 px-5 py-2">
									Quantity
								</th>
								<th className="text-left text-xs font-medium text-slate-400 px-5 py-2">
									Unit
								</th>
							</tr>
						</thead>
						<tbody>
							{items.map((item, index) => (
								<tr
									key={index}
									className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
									<td className="px-5 py-3 text-sm font-medium text-slate-800">
										{item.itemName}
									</td>
									<td className="px-5 py-3">
										<span className="text-xs bg-blue-50 text-blue-700 px-2 py-1 rounded-full">
											{item.portionSize}
										</span>
									</td>
									<td className="px-5 py-3">
										<span className="text-2xl font-bold text-slate-800">
											{item.totalQuantity}
										</span>
									</td>
									<td className="px-5 py-3 text-sm text-slate-400">
										{item.unit}
									</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>
			))}

			{/* Total boxes count */}
			<div className="bg-blue-600 rounded-xl p-5 text-white flex items-center justify-between">
				<div>
					<p className="text-blue-200 text-sm">Total tiffin boxes to pack</p>
					<p className="text-4xl font-bold mt-1">{summary.totalBoxes}</p>
				</div>
				<ChefHat size={48} className="text-blue-400" />
			</div>
		</div>
	);
}
