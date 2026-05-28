"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import api from "@/lib/api";
import { PlanListItem } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import Navbar from "@/components/customer/Navbar";
import { Check, Leaf, Clock, MapPin, Star } from "lucide-react";

export default function HomePage() {
	const tenantSlug = process.env.NEXT_PUBLIC_TENANT_SLUG ?? "TiffinOS";

	useEffect(() => {
		localStorage.setItem("tenantSlug", tenantSlug);
	}, [tenantSlug]);

	const { data: plans = [], isLoading } = useQuery<PlanListItem[]>({
		queryKey: ["publicPlans"],
		queryFn: async () => api.get("api/plans").then((r) => r.data),
	});

	return (
		<div>
			<Navbar />

			{/* Hero Section */}
			<section className="max-w-5xl mx-auto px-6 py-20 text-center">
				<Badge className="bg-green-100 text-green-700 hover:bg-green-100 mb-4">
					🍱 Fresh tiffins delivered daily
				</Badge>
				<h1 className="text-5xl font-bold text-slate-800 leading-tight mt-4">
					Home-cooked meals,
					<br />
					<span className="text-blue-600">delivered to your door</span>
				</h1>
				<p className="text-slate-500 mt-6 text-lg max-w-xl mx-auto">
					Subscribe to a tiffin plan and get fresh, homemade food delivered
					every day. No cooking, no stress.
				</p>
				<div className="flex items-center justify-center gap-3 mt-8">
					<Link href="/home#plans">
						<Button
							size="lg"
							className="bg-blue-600 hover:bg-blue-700 h-12 px-8">
							View Plans & Pricing
						</Button>
					</Link>
					<Link href="/home#how-it-works">
						<Button size="lg" variant="outline" className="h-12 px-8">
							How it works
						</Button>
					</Link>
				</div>

				{/* Trust signals */}
				<div className="flex items-center justify-center gap-8 mt-12 text-sm text-slate-400">
					<div className="flex items-center gap-2">
						<Check size={16} className="text-green-500" />
						No long-term commitment
					</div>
					<div className="flex items-center gap-2">
						<Check size={16} className="text-green-500" />
						Skip any day
					</div>
					<div className="flex items-center gap-2">
						<Check size={16} className="text-green-500" />
						Cancel anytime
					</div>
				</div>
			</section>

			{/* Plans Section */}
			<section id="plans" className="bg-slate-50 py-16">
				<div className="max-w-5xl mx-auto px-6">
					<div className="text-center mb-10">
						<h2 className="text-3xl font-bold text-slate-800">
							Choose your plan
						</h2>
						<p className="text-slate-500 mt-2">
							Save more when you subscribe longer
						</p>
					</div>

					{isLoading ? (
						<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
							{Array.from({ length: 2 }).map((_, i) => (
								<Skeleton key={i} className="h-64 rounded-2xl" />
							))}
						</div>
					) : (
						<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
							{plans.map((plan) => (
								<PlanCard key={plan.id} plan={plan} />
							))}
						</div>
					)}
				</div>
			</section>

			{/* How It Works */}
			<section id="how-it-works" className="py-16">
				<div className="max-w-5xl mx-auto px-6">
					<div className="text-center mb-10">
						<h2 className="text-3xl font-bold text-slate-800">How it works</h2>
					</div>
					<div className="grid grid-cols-1 md:grid-cols-3 gap-8">
						{[
							{
								icon: <Leaf size={24} className="text-green-500" />,
								step: "1",
								title: "Choose a plan",
								desc: "Pick the tiffin plan that suits you — daily, weekly, or monthly subscription.",
							},
							{
								icon: <MapPin size={24} className="text-blue-500" />,
								step: "2",
								title: "Enter your address",
								desc: "We check if your address is within our delivery zone and confirm your slot.",
							},
							{
								icon: <Clock size={24} className="text-amber-500" />,
								step: "3",
								title: "Get it delivered",
								desc: "Fresh tiffin delivered to your door every day. Track your delivery in real time.",
							},
						].map((item) => (
							<div key={item.step} className="text-center">
								<div className="w-12 h-12 rounded-2xl bg-slate-100 flex items-center justify-center mx-auto mb-4">
									{item.icon}
								</div>
								<p className="font-semibold text-slate-800 mb-2">
									{item.title}
								</p>
								<p className="text-slate-500 text-sm leading-relaxed">
									{item.desc}
								</p>
							</div>
						))}
					</div>
				</div>
			</section>

			{/* Footer */}
			<footer className="border-t border-slate-100 py-8">
				<div className="max-w-5xl mx-auto px-6 flex items-center justify-between text-sm text-slate-400">
					<p>© 2026 {tenantSlug}. Powered by TiffinOS.</p>
					<div className="flex gap-4">
						<Link href="/customer-login" className="hover:text-slate-600">
							Sign in
						</Link>
					</div>
				</div>
			</footer>
		</div>
	);
}

function PlanCard({ plan }: { plan: PlanListItem }) {
	return (
		<div className="bg-white rounded-2xl border border-slate-200 p-6 hover:border-blue-300 hover:shadow-md transition-all">
			{/* Header */}
			<div className="flex items-start justify-between mb-4">
				<div>
					<h3 className="font-bold text-slate-800 text-lg">{plan.name}</h3>
					<Badge
						className={
							plan.dietaryType === "veg"
								? "bg-green-100 text-green-700 hover:bg-green-100 mt-1"
								: "bg-red-100 text-red-700 hover:bg-red-100 mt-1"
						}>
						{plan.dietaryType === "veg" ? "🌱 Veg" : "🍖 Non-veg"}
					</Badge>
				</div>
				<div className="text-right">
					<p className="text-xs text-slate-400">From</p>
					<p className="text-2xl font-bold text-blue-600">
						${plan.lowestPrice}
						<span className="text-sm font-normal text-slate-400">/day</span>
					</p>
				</div>
			</div>

			{/* Stats */}
			<div className="flex items-center gap-4 text-xs text-slate-400 mb-5">
				<span>{plan.itemCount} items included</span>
				<span>{plan.tierCount} duration options</span>
			</div>

			{/* CTA */}
			<Link href={`/checkout?planId=${plan.id}`}>
				<Button className="w-full bg-blue-600 hover:bg-blue-700">
					Subscribe
				</Button>
			</Link>
		</div>
	);
}
