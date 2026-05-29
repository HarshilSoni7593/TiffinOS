"use client";

import { useState, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import Link from "next/link";
import api from "@/lib/api";
import { useAuthStore } from "@/store/authStore";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import Navbar from "@/components/customer/Navbar";
import { toast } from "sonner";
import { Check, MapPin, AlertCircle } from "lucide-react";
import { cn } from "@/lib/utils";

export default function CheckoutPage() {
	const router = useRouter();
	const searchParams = useSearchParams();
	const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
	const planId = searchParams.get("planId") ?? "";

	const [selectedTierId, setSelectedTierId] = useState("");
	const [deliveryAddress, setDeliveryAddress] = useState("");
	const [floorOrUnit, setFloorOrUnit] = useState("");
	const [instructions, setInstructions] = useState("");
	const [spicePreference, setSpicePreference] = useState("");
	const [startDate, setStartDate] = useState("");
	const [zoneId, setZoneId] = useState("");
	const [zoneName, setZoneName] = useState("");
	const [zoneLoading, setZoneLoading] = useState(false);
	const [zoneError, setZoneError] = useState("");
	const [deliveryLat, setDeliveryLat] = useState(0);
	const [deliveryLng, setDeliveryLng] = useState(0);
	const [step, setStep] = useState<1 | 2 | 3>(1);

	useEffect(() => {
		const tomorrow = new Date();
		tomorrow.setDate(tomorrow.getDate() + 1);
		setStartDate(tomorrow.toISOString().split("T")[0]);
	}, []);

	useEffect(() => {
		if (!isAuthenticated) {
			router.push("/customer-login");
		}
	}, [isAuthenticated, router]);

	const { data: plan, isLoading: planLoading } = useQuery<any>({
		queryKey: ["plan", planId],
		queryFn: () => api.get(`/api/plans/${planId}`).then((r) => r.data),
		enabled: !!planId,
	});

	const selectedTier = plan?.pricingTiers?.find(
		(t: any) => t.id === selectedTierId,
	);

	const { data: chargePreview } = useQuery<any>({
		queryKey: ["chargePreview", zoneId, selectedTierId],
		queryFn: () =>
			api
				.post("/api/delivery-charges/preview", {
					zoneId,
					pricingTierId: selectedTierId,
					pricePerDay: selectedTier?.pricePerDay ?? 0,
				})
				.then((r) => r.data),
		enabled: !!zoneId && !!selectedTierId,
	});

	const resolveZone = async (address: string) => {
		if (!address.trim()) return;
		setZoneLoading(true);
		setZoneError("");

		try {
			const lat = 43.651;
			const lng = -79.375;

			setDeliveryLat(lat);
			setDeliveryLng(lng);

			const response = await api.post("/api/zones/resolve", { lat, lng });
			const data = response.data;

			if (data.zoneId) {
				setZoneId(data.zoneId);
				setZoneName(data.zoneName);
			} else {
				setZoneId("");
				setZoneName("");
				setZoneError(
					"Sorry, we do not deliver to this address. Please enter a different address within our delivery area.",
				);
			}
		} catch {
			setZoneError("Could not verify delivery zone, please try again later.");
		} finally {
			setZoneLoading(false);
		}
	};

	const subscribe = useMutation({
		mutationFn: () =>
			api.post("/api/subscriptions", {
				planId: planId,
				pricingTierId: selectedTierId,
				zoneId,
				deliveryAddress: deliveryAddress.trim(),
				deliveryLat,
				deliveryLng,
				floorOrUnit: floorOrUnit || null,
				deliveryInstructions: instructions || null,
				spicePreference: spicePreference || null,
				startDate,
			}),
		onSuccess: (response) => {
			toast.success("Subscription created successfully");
			router.push("/my-subscription");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Subscription failed"),
	});

	if (!planId) {
		return (
			<div className="min-h-screen bg-slate-50 flex items-center justify-center">
				<div className="text-center">
					<p className="text-slate-500 mb-4">No plan selected.</p>
					<Link href="/home#plans">
						<Button className="bg-blue-600 hover:bg-blue-700">
							Browse Plans
						</Button>
					</Link>
				</div>
			</div>
		);
	}

	return (
		<div className="min-h-screen bg-slate-50">
			<Navbar />

			<div className="max-w-2xl mx-auto px-6 py-10">
				{/* Page title */}
				<div className="mb-8">
					<h1 className="text-2xl font-bold text-slate-800">
						Complete your subscription
					</h1>
					<p className="text-slate-500 text-sm mt-1">
						You are just a few steps away from fresh daily tiffins
					</p>
				</div>

				{/* Step indicator */}
				<div className="flex items-center gap-2 mb-8">
					{[
						{ n: 1, label: "Choose duration" },
						{ n: 2, label: "Delivery details" },
						{ n: 3, label: "Confirm" },
					].map(({ n, label }) => (
						<div key={n} className="flex items-center gap-2">
							<div
								className={cn(
									"w-7 h-7 rounded-full flex items-center justify-center text-xs font-semibold transition-colors",
									step === n
										? "bg-blue-600 text-white"
										: step > n
											? "bg-green-500 text-white"
											: "bg-slate-200 text-slate-500",
								)}>
								{step > n ? <Check size={12} /> : n}
							</div>
							<span
								className={cn(
									"text-sm hidden sm:block",
									step === n ? "text-slate-800 font-medium" : "text-slate-400",
								)}>
								{label}
							</span>
							{n < 3 && <div className="w-8 h-px bg-slate-200 mx-1" />}
						</div>
					))}
				</div>

				{/* Plan summary card */}
				{planLoading ? (
					<Skeleton className="h-24 rounded-2xl mb-6" />
				) : (
					plan && (
						<div className="bg-white rounded-2xl border border-slate-200 p-4 mb-6 flex items-center justify-between">
							<div>
								<p className="font-semibold text-slate-800">{plan.name}</p>
								<p className="text-xs text-slate-400 mt-0.5">
									{plan.items?.length} items · {plan.dietaryType}
								</p>
							</div>
							{selectedTier && (
								<div className="text-right">
									<p className="text-xl font-bold text-blue-600">
										${selectedTier.totalAmount}
									</p>
									<p className="text-xs text-slate-400 capitalize">
										per {selectedTier.durationType}
									</p>
								</div>
							)}
						</div>
					)
				)}

				{/* Step 1 — Choose Duration */}
				{step === 1 && (
					<div className="bg-white rounded-2xl border border-slate-200 p-6 space-y-4">
						<h2 className="font-semibold text-slate-800">
							Choose your subscription duration
						</h2>

						<div className="space-y-3">
							{plan?.pricingTiers?.map((tier: any) => (
								<button
									key={tier.id}
									type="button"
									onClick={() => setSelectedTierId(tier.id)}
									className={cn(
										"w-full flex items-center justify-between p-4 rounded-xl border-2 transition-all text-left",
										selectedTierId === tier.id
											? "border-blue-600 bg-blue-50"
											: "border-slate-200 hover:border-slate-300",
									)}>
									<div>
										<p className="font-medium text-slate-800 capitalize">
											{tier.durationType}
										</p>
										<p className="text-xs text-slate-400 mt-0.5">
											${tier.pricePerDay}/day effective rate
										</p>
									</div>
									<div className="text-right">
										<p className="text-lg font-bold text-slate-800">
											${tier.totalAmount}
										</p>
										<p className="text-xs text-slate-400">
											every {tier.billingCycleDays} day
											{tier.billingCycleDays > 1 ? "s" : ""}
										</p>
									</div>
								</button>
							))}
						</div>

						<div className="space-y-2 pt-2">
							<Label>Start date</Label>
							<input
								type="date"
								value={startDate}
								min={
									new Date(Date.now() + 86400000).toISOString().split("T")[0]
								}
								onChange={(e) => setStartDate(e.target.value)}
								className="w-full h-9 px-3 rounded-lg border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
							/>
						</div>

						<Button
							className="w-full bg-blue-600 hover:bg-blue-700"
							disabled={!selectedTierId || !startDate}
							onClick={() => setStep(2)}>
							Next: Delivery Details →
						</Button>
					</div>
				)}

				{/* Step 2 — Delivery Details */}
				{step === 2 && (
					<div className="bg-white rounded-2xl border border-slate-200 p-6 space-y-4">
						<h2 className="font-semibold text-slate-800">
							Where should we deliver?
						</h2>

						<div className="space-y-2">
							<Label>Delivery Address *</Label>
							<div className="flex gap-2">
								<Input
									placeholder="123 King Street West, Toronto, ON"
									value={deliveryAddress}
									onChange={(e) => setDeliveryAddress(e.target.value)}
									className="flex-1"
								/>
								<Button
									type="button"
									variant="outline"
									onClick={() => resolveZone(deliveryAddress)}
									disabled={zoneLoading || !deliveryAddress.trim()}>
									<MapPin size={14} className="mr-1" />
									{zoneLoading ? "Checking..." : "Check zone"}
								</Button>
							</div>

							{/* Zone result */}
							{zoneName && (
								<div className="flex items-center gap-2 text-sm text-green-600 bg-green-50 px-3 py-2 rounded-lg">
									<Check size={14} />
									Delivery available — {zoneName}
								</div>
							)}
							{zoneError && (
								<div className="flex items-center gap-2 text-sm text-red-600 bg-red-50 px-3 py-2 rounded-lg">
									<AlertCircle size={14} />
									{zoneError}
								</div>
							)}
						</div>

						<div className="space-y-2">
							<Label>Floor / Unit (optional)</Label>
							<Input
								placeholder="Apt 4B, Unit 2, Floor 3"
								value={floorOrUnit}
								onChange={(e) => setFloorOrUnit(e.target.value)}
							/>
						</div>

						<div className="space-y-2">
							<Label>Delivery Instructions (optional)</Label>
							<Input
								placeholder="Leave at door, ring bell, etc."
								value={instructions}
								onChange={(e) => setInstructions(e.target.value)}
							/>
						</div>

						<div className="space-y-2">
							<Label>Spice Preference (optional)</Label>
							<Select
								value={spicePreference}
								onValueChange={setSpicePreference}>
								<SelectTrigger>
									<SelectValue placeholder="Select preference" />
								</SelectTrigger>
								<SelectContent>
									<SelectItem value="mild">🟡 Mild</SelectItem>
									<SelectItem value="medium">🟠 Medium</SelectItem>
									<SelectItem value="spicy">🔴 Spicy</SelectItem>
								</SelectContent>
							</Select>
						</div>

						<div className="flex gap-3 pt-2">
							<Button
								variant="outline"
								onClick={() => setStep(1)}
								className="flex-1">
								← Back
							</Button>
							<Button
								className="flex-1 bg-blue-600 hover:bg-blue-700"
								disabled={!zoneId || !deliveryAddress.trim()}
								onClick={() => setStep(3)}>
								Next: Confirm →
							</Button>
						</div>
					</div>
				)}

				{/* Step 3 — Confirm */}
				{step === 3 && (
					<div className="bg-white rounded-2xl border border-slate-200 p-6 space-y-5">
						<h2 className="font-semibold text-slate-800">Review your order</h2>

						{/* Summary rows */}
						<div className="space-y-3 text-sm">
							<div className="flex justify-between">
								<span className="text-slate-500">Plan</span>
								<span className="font-medium text-slate-800">{plan?.name}</span>
							</div>
							<div className="flex justify-between">
								<span className="text-slate-500">Duration</span>
								<span className="font-medium text-slate-800 capitalize">
									{selectedTier?.durationType}
								</span>
							</div>
							<div className="flex justify-between">
								<span className="text-slate-500">Start date</span>
								<span className="font-medium text-slate-800">{startDate}</span>
							</div>
							<div className="flex justify-between">
								<span className="text-slate-500">Delivery to</span>
								<span className="font-medium text-slate-800 text-right max-w-xs">
									{deliveryAddress}
									{floorOrUnit && `, ${floorOrUnit}`}
								</span>
							</div>
							<div className="flex justify-between">
								<span className="text-slate-500">Zone</span>
								<span className="font-medium text-slate-800">{zoneName}</span>
							</div>
							{spicePreference && (
								<div className="flex justify-between">
									<span className="text-slate-500">Spice</span>
									<span className="font-medium text-slate-800 capitalize">
										{spicePreference}
									</span>
								</div>
							)}
						</div>

						{/* Price breakdown */}
						<div className="border-t border-slate-100 pt-4 space-y-2">
							<div className="flex justify-between text-sm">
								<span className="text-slate-500">Plan cost</span>
								<span className="text-slate-800">
									${selectedTier?.totalAmount}
								</span>
							</div>
							<div className="flex justify-between text-sm">
								<span className="text-slate-500">Delivery</span>
								<span
									className={
										chargePreview?.deliveryCharge === 0
											? "text-green-600 font-medium"
											: "text-slate-800"
									}>
									{chargePreview?.deliveryCharge === 0
										? "Free"
										: `$${chargePreview?.deliveryCharge}`}
								</span>
							</div>
							<div className="flex justify-between font-semibold text-base border-t border-slate-100 pt-2">
								<span className="text-slate-800">Total due now</span>
								<span className="text-blue-600">
									$
									{(
										(selectedTier?.totalAmount ?? 0) +
										(chargePreview?.deliveryCharge ?? 0)
									).toFixed(2)}{" "}
									CAD
								</span>
							</div>
						</div>

						<div className="flex gap-3">
							<Button
								variant="outline"
								onClick={() => setStep(2)}
								className="flex-1">
								← Back
							</Button>
							<Button
								className="flex-1 bg-blue-600 hover:bg-blue-700"
								onClick={() => subscribe.mutate()}
								disabled={subscribe.isPending}>
								{subscribe.isPending ? "Processing..." : "Confirm & Subscribe"}
							</Button>
						</div>

						<p className="text-xs text-slate-400 text-center">
							Payment will be collected separately. You can cancel anytime from
							your dashboard.
						</p>
					</div>
				)}
			</div>
		</div>
	);
}
