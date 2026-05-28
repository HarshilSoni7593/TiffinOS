"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";
import { useAuthStore } from "@/store/authStore";

export default function Navbar() {
	const tenantName = process.env.NEXT_PUBLIC_TENANT_SLUG ?? "TiffinOS";
	const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
	const user = useAuthStore((s) => s.user);
	const isCustomer = user?.roles.includes("customer") ?? false;

	return (
		<nav className="border-b border-slate-100 bg-white sticky top-0 z-50">
			<div className="max-w-5xl mx-auto px-6 h-16 flex items-center justify-between">
				{/* Logo */}
				<Link
					href="/home"
					className="font-bold text-xl text-slate-800 capitalize">
					{tenantName}
				</Link>

				{/* Links */}
				<div className="flex items-center gap-6">
					<Link
						href="/home#plans"
						className="text-sm text-slate-500 hover:text-slate-800 transition-colors">
						Plans
					</Link>
					<Link
						href="/home#how-it-works"
						className="text-sm text-slate-500 hover:text-slate-800 transition-colors">
						How it works
					</Link>

					{isAuthenticated && isCustomer ? (
						<Link href="/my-subscription">
							<Button size="sm" className="bg-blue-600 hover:bg-blue-700">
								My Subscription
							</Button>
						</Link>
					) : (
						<div className="flex items-center gap-2">
							<Link href="/customer-login">
								<Button size="sm" variant="outline">
									Sign in
								</Button>
							</Link>
							<Link href="/home#plans">
								<Button size="sm" className="bg-blue-600 hover:bg-blue-700">
									Subscribe Now
								</Button>
							</Link>
						</div>
					)}
				</div>
			</div>
		</nav>
	);
}
