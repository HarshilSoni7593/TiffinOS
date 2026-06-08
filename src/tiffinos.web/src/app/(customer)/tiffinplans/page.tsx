"use client";

import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import Link from "next/link";
import api from "@/lib/api";
import Navbar from "@/components/customer/Navbar";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

export default function TiffinPlansPage() {
	const tenantSlug = process.env.NEXT_PUBLIC_TENANT_SLUG ?? "";

	// Ensure tenant slug is set for API calls
	useEffect(() => {
		localStorage.setItem("tenantSlug", tenantSlug);
	}, [tenantSlug]);

	const { data: plans = [], isLoading } = useQuery<any[]>({
		queryKey: ["publicPlans"],
		queryFn: () => api.get("/api/plans").then((r) => r.data),
	});

	return (
		<div className="min-h-screen bg-slate-50">
			<Navbar />

			{/* Header */}
			<section className="bg-white border-b border-slate-100 py-12">
				<div className="max-w-5xl mx-auto px-6 text-center">
					<h1 className="text-4xl font-bold text-slate-800">
						Our Tiffin Plans
					</h1>
					<p className="text-slate-500 mt-3 text-lg">
						Fresh homemade food delivered daily. Save more when you subscribe
						longer.
					</p>
				</div>
			</section>

			{/* Plans */}
			<section className="max-w-5xl mx-auto px-6 py-12">
				{isLoading ? (
					<div className="space-y-6">
						{Array.from({ length: 2 }).map((_, i) => (
							<Skeleton key={i} className="h-64 w-full rounded-2xl" />
						))}
					</div>
				) : plans.length === 0 ? (
					<div className="text-center py-20">
						<p className="text-slate-400">
							No plans available right now. Check back soon.
						</p>
					</div>
				) : (
					<div className="space-y-6">
						{plans.map((plan) => (
							<PlanDetailCard key={plan.id} planId={plan.id} />
						))}
					</div>
				)}
			</section>

			{/* Footer */}
			<footer className="border-t border-slate-100 py-8 mt-8">
				<div className="max-w-5xl mx-auto px-6 flex items-center justify-between text-sm text-slate-400">
					<p>© 2026 {tenantSlug}. Powered by TiffinOS.</p>
					<Link href="/home" className="hover:text-slate-600">
						← Back to home
					</Link>
				</div>
			</footer>
		</div>
	);
}

function PlanDetailCard({ planId }: { planId: string }) {
	const { data: plan, isLoading } = useQuery<any>({
		queryKey: ["plan", planId],
		queryFn: () => api.get(`/api/plans/${planId}`).then((r) => r.data),
	});

	if (isLoading) return <Skeleton className="h-64 w-full rounded-2xl" />;
	if (!plan) return null;

	return (
		<div className="bg-white rounded-2xl border border-slate-200 overflow-hidden hover:shadow-md transition-shadow">
			{/* Plan Header */}
			<div className="px-8 py-6 border-b border-slate-100">
				<div className="flex items-start justify-between">
					<div>
						<div className="flex items-center gap-3 mb-2">
							<h2 className="text-2xl font-bold text-slate-800">{plan.name}</h2>
							<Badge
								className={cn(
									"text-xs",
									plan.dietaryType === "veg"
										? "bg-green-100 text-green-700 hover:bg-green-100"
										: "bg-red-100 text-red-700 hover:bg-red-100",
								)}>
								{plan.dietaryType === "veg" ? "🌱 Veg" : "🍖 Non-veg"}
							</Badge>
						</div>
						{plan.description && (
							<p className="text-slate-500 text-sm">{plan.description}</p>
						)}
						<div className="flex items-center gap-4 mt-3 text-xs text-slate-400">
							{plan.boxType && <span>📦 {plan.boxType.replace("_", " ")}</span>}
							{plan.allowSkip && <span>⏸ Skip days allowed</span>}
						</div>
					</div>

					<Link href={`/checkout?planId=${plan.id}`}>
						<Button className="bg-blue-600 hover:bg-blue-700 px-8">
							Subscribe
						</Button>
					</Link>
				</div>
			</div>

			<div className="px-8 py-6 grid grid-cols-1 md:grid-cols-2 gap-8">
				{/* What's Included */}
				<div>
					<p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-4">
						What's included
					</p>
					<div className="space-y-2">
						{plan.items
							?.sort((a: any, b: any) => a.displayOrder - b.displayOrder)
							.map((item: any) => (
								<div key={item.id} className="flex items-center gap-3">
									<div className="w-5 h-5 rounded-full bg-green-100 flex items-center justify-center flex-shrink-0">
										<Check size={11} className="text-green-600" />
									</div>
									<span className="text-sm text-slate-700">
										{item.quantity}× {item.menuItemName}
									</span>
									<Badge
										variant="secondary"
										className="text-xs bg-slate-100 text-slate-500 ml-auto">
										{item.portionSize}
									</Badge>
								</div>
							))}
					</div>
				</div>

				{/* Pricing Tiers */}
				<div>
					<p className="text-xs font-semibold text-slate-500 uppercase tracking-wide mb-4">
						Pricing options
					</p>
					<div className="space-y-3">
						{plan.pricingTiers?.map((tier: any, index: number) => (
							<div
								key={tier.id}
								className={cn(
									"flex items-center justify-between p-3 rounded-xl border",
									index === plan.pricingTiers.length - 1
										? "border-blue-200 bg-blue-50"
										: "border-slate-100 bg-slate-50",
								)}>
								<div>
									<p className="text-sm font-medium text-slate-800 capitalize">
										{tier.durationType}
									</p>
									<p className="text-xs text-slate-400">
										${tier.pricePerDay}/day effective
									</p>
								</div>
								<div className="text-right">
									<p className="text-lg font-bold text-slate-800">
										${tier.totalAmount}
									</p>
									{index === plan.pricingTiers.length - 1 && (
										<p className="text-xs text-blue-600 font-medium">
											Best value
										</p>
									)}
								</div>
							</div>
						))}
					</div>

					<Link href={`/checkout?planId=${plan.id}`} className="block mt-4">
						<Button
							variant="outline"
							className="w-full border-blue-200 text-blue-600 hover:bg-blue-50">
							Choose this plan →
						</Button>
					</Link>
				</div>
			</div>
		</div>
	);
}
