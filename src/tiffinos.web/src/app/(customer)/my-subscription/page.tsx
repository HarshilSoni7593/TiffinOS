"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import api from "@/lib/api";
import { useAuthStore } from "@/store/authStore";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import Navbar from "@/components/customer/Navbar";
import { toast } from "sonner";
import {
	Package,
	MapPin,
	Calendar,
	CheckCircle,
	PauseCircle,
	XCircle,
	LogOut,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/hooks/useAuth";

export default function MySubscriptionPage() {
	const router = useRouter();
	const { logout } = useAuth();
	const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
	const user = useAuthStore((s) => s.user);
	const queryClient = useQueryClient();
	const [hydrated, setHydrated] = useState(false);

	useEffect(() => {
		setHydrated(true);
	}, []);

	useEffect(() => {
		if (hydrated && !isAuthenticated) {
			router.push("/customer-login");
		}
	}, [hydrated, isAuthenticated, router]);

	const { data: subscriptions = [], isLoading } = useQuery<any[]>({
		queryKey: ["mySubscriptions"],
		queryFn: () => api.get("/api/subscriptions/my").then((r) => r.data),
		enabled: isAuthenticated,
	});

	const pauseSub = useMutation({
		mutationFn: ({ id, pauseUntil }: { id: string; pauseUntil: string }) =>
			api.post(`/api/subscriptions/${id}/pause`, {
				pauseUntil,
				reason: "Customer requested pause",
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["mySubscriptions"] });
			toast.success("Subscription paused successfully");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to pause"),
	});

	const cancelSub = useMutation({
		mutationFn: (id: string) =>
			api.post(`/api/subscriptions/${id}/cancel`, {
				reason: "Customer requested cancellation",
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["mySubscriptions"] });
			toast.success("Subscription cancelled");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to cancel"),
	});

	if (!hydrated) return null;

	return (
		<div className="min-h-screen bg-slate-50">
			<Navbar />

			<div className="max-w-3xl mx-auto px-6 py-10 space-y-6">
				{/* Header */}
				<div className="flex items-center justify-between">
					<div>
						<h1 className="text-2xl font-bold text-slate-800">
							My Subscription
						</h1>
						<p className="text-slate-500 text-sm mt-1">
							{user?.firstName}, here is your delivery overview
						</p>
					</div>
					<Button
						variant="outline"
						size="sm"
						className="text-slate-500"
						onClick={logout}>
						<LogOut size={14} className="mr-2" />
						Sign out
					</Button>
				</div>

				{/* Subscriptions */}
				{isLoading ? (
					<div className="space-y-4">
						{Array.from({ length: 2 }).map((_, i) => (
							<Skeleton key={i} className="h-40 rounded-2xl" />
						))}
					</div>
				) : subscriptions.length === 0 ? (
					<div className="bg-white rounded-2xl border border-slate-200 p-12 text-center">
						<Package size={40} className="text-slate-300 mx-auto mb-4" />
						<h2 className="text-lg font-semibold text-slate-700 mb-2">
							No active subscription
						</h2>
						<p className="text-slate-400 text-sm mb-6">
							You have not subscribed to any tiffin plan yet.
						</p>
						<Link href="/home#plans">
							<Button className="bg-blue-600 hover:bg-blue-700">
								Browse Plans
							</Button>
						</Link>
					</div>
				) : (
					<div className="space-y-4">
						{subscriptions.map((sub: any) => (
							<SubscriptionCard
								key={sub.id}
								sub={sub}
								onPause={(pauseUntil) =>
									pauseSub.mutate({ id: sub.id, pauseUntil })
								}
								onCancel={() => {
									if (confirm("Are you sure you want to cancel?"))
										cancelSub.mutate(sub.id);
								}}
								pauseLoading={pauseSub.isPending}
								cancelLoading={cancelSub.isPending}
							/>
						))}
					</div>
				)}

				{/* Browse more plans */}
				{subscriptions.length > 0 && (
					<div className="text-center pt-4">
						<Link
							href="/home#plans"
							className="text-sm text-blue-600 hover:underline">
							Browse other plans →
						</Link>
					</div>
				)}
			</div>
		</div>
	);
}

const statusConfig: Record<
	string,
	{
		label: string;
		className: string;
		icon: React.ReactNode;
	}
> = {
	active: {
		label: "Active",
		className: "bg-green-100 text-green-700",
		icon: <CheckCircle size={14} />,
	},
	paused: {
		label: "Paused",
		className: "bg-amber-100 text-amber-700",
		icon: <PauseCircle size={14} />,
	},
	cancelled: {
		label: "Cancelled",
		className: "bg-red-100 text-red-700",
		icon: <XCircle size={14} />,
	},
	pending: {
		label: "Pending Payment",
		className: "bg-slate-100 text-slate-500",
		icon: <Package size={14} />,
	},
};

function SubscriptionCard({
	sub,
	onPause,
	onCancel,
	pauseLoading,
	cancelLoading,
}: {
	sub: any;
	onPause: (pauseUntil: string) => void;
	onCancel: () => void;
	pauseLoading: boolean;
	cancelLoading: boolean;
}) {
	const status = statusConfig[sub.status] ?? {
		label: sub.status,
		className: "bg-slate-100 text-slate-500",
		icon: null,
	};

	const handlePause = () => {
		// Pause for 7 days by default
		const pauseUntil = new Date();
		pauseUntil.setDate(pauseUntil.getDate() + 7);
		onPause(pauseUntil.toISOString().split("T")[0]);
	};

	return (
		<div className="bg-white rounded-2xl border border-slate-200 overflow-hidden">
			{/* Card Header */}
			<div className="px-6 py-5 flex items-start justify-between">
				<div>
					<div className="flex items-center gap-2 mb-1">
						<h3 className="font-bold text-slate-800 text-lg">
							{sub.plan.name}
						</h3>
						<Badge
							variant="secondary"
							className={cn(
								"flex items-center gap-1 text-xs",
								status.className,
							)}>
							{status.icon}
							{status.label}
						</Badge>
					</div>
					<p className="text-slate-500 text-sm capitalize">
						{sub.pricingTier.durationType} plan
					</p>
				</div>
				<div className="text-right">
					<p className="text-2xl font-bold text-slate-800">
						${sub.lockedTotalAmount}
					</p>
					<p className="text-xs text-slate-400 capitalize">
						per {sub.pricingTier.durationType}
					</p>
					{sub.lockedDeliveryCharge === 0 ? (
						<p className="text-xs text-green-600 mt-0.5">Free delivery</p>
					) : (
						<p className="text-xs text-slate-400 mt-0.5">
							+${sub.lockedDeliveryCharge} delivery
						</p>
					)}
				</div>
			</div>

			{/* Details */}
			<div className="px-6 pb-4 grid grid-cols-2 gap-4">
				<div className="flex items-start gap-2">
					<Calendar size={14} className="text-slate-400 mt-0.5 flex-shrink-0" />
					<div>
						<p className="text-xs text-slate-400">Period</p>
						<p className="text-sm text-slate-700">
							{sub.startDate}
							{sub.endDate && ` → ${sub.endDate}`}
						</p>
					</div>
				</div>
				<div className="flex items-start gap-2">
					<MapPin size={14} className="text-slate-400 mt-0.5 flex-shrink-0" />
					<div>
						<p className="text-xs text-slate-400">Delivery address</p>
						<p className="text-sm text-slate-700 leading-snug">
							{sub.deliveryAddress}
							{sub.floorOrUnit && `, ${sub.floorOrUnit}`}
						</p>
					</div>
				</div>
			</div>

			{/* What's included */}
			<div className="px-6 pb-4">
				<p className="text-xs text-slate-400 mb-2">
					{sub.zone.name} · Spice: {sub.spicePreference ?? "Not specified"}
				</p>
			</div>

			{/* Actions */}
			{(sub.status === "active" || sub.status === "paused") && (
				<div className="px-6 py-4 border-t border-slate-100 flex items-center gap-3">
					{sub.status === "active" && sub.plan.allowSkip && (
						<Button
							variant="outline"
							size="sm"
							className="h-8 text-xs"
							onClick={handlePause}
							disabled={pauseLoading}>
							<PauseCircle size={14} className="mr-1.5" />
							Pause 7 days
						</Button>
					)}
					{sub.status === "paused" && (
						<p className="text-xs text-amber-600">
							Paused until {sub.pausedUntil}
						</p>
					)}
					<Button
						variant="outline"
						size="sm"
						className="h-8 text-xs text-red-600 border-red-200 hover:bg-red-50"
						onClick={onCancel}
						disabled={cancelLoading}>
						<XCircle size={14} className="mr-1.5" />
						Cancel subscription
					</Button>
				</div>
			)}
		</div>
	);
}
