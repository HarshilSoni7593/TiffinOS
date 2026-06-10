"use client";

import { useState, useRef, useEffect } from "react";
import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { useAuthStore } from "@/store/authStore";
import { LogOut, User } from "lucide-react";

const pageTitles: Record<string, string> = {
	"/dashboard": "Dashboard",
	"/menu-items": "Menu Items",
	"/plans": "Tiffin Plans",
	"/zones": "Delivery Zones",
	"/subscriptions": "Subscriptions",
	"/drivers": "Drivers",
	"/routes": "Routes",
	"/payouts": "Payouts",
	"/packing": "Packing Summary",
	"/setup/business": "Business Settings",
	"/setup/pricing-tiers": "Pricing Tiers",
	"/setup/delivery-charges": "Delivery Charges",
	"/setup/holidays": "Holidays",
	"/setup/payout-policies": "Payout Policies",
};

export default function Header() {
	const pathname = usePathname();
	const { logout } = useAuth();
	const user = useAuthStore((s) => s.user);
	const [open, setOpen] = useState(false);
	const ref = useRef<HTMLDivElement>(null);

	const title =
		pageTitles[pathname] ??
		pageTitles[
			Object.keys(pageTitles).find((k) => pathname.startsWith(k)) ?? ""
		] ??
		"TiffinOS";

	// Close dropdown when clicking outside
	useEffect(() => {
		const handleClick = (e: MouseEvent) => {
			if (ref.current && !ref.current.contains(e.target as Node))
				setOpen(false);
		};
		document.addEventListener("mousedown", handleClick);
		return () => document.removeEventListener("mousedown", handleClick);
	}, []);

	return (
		<header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6 flex-shrink-0">
			<h1 className="text-lg font-semibold text-slate-800">{title}</h1>

			<div className="relative" ref={ref}>
				<button
					onClick={() => setOpen((prev) => !prev)}
					className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-slate-100 transition-colors">
					<div className="w-7 h-7 rounded-full bg-blue-600 flex items-center justify-center">
						<span className="text-white text-xs font-semibold">
							{user?.firstName?.charAt(0).toUpperCase()}
						</span>
					</div>
					<span className="text-sm font-medium text-slate-700">
						{user?.firstName}
					</span>
				</button>

				{open && (
					<div className="absolute right-0 top-full mt-1 w-52 bg-white rounded-xl border border-slate-200 shadow-lg z-50 py-1 overflow-hidden">
						{/* User info */}
						<div className="px-4 py-3 border-b border-slate-100">
							<p className="text-sm font-medium text-slate-800">
								{user?.firstName}
							</p>
							<p className="text-xs text-slate-500 mt-0.5">{user?.email}</p>
						</div>

						{/* Profile */}
						<button className="w-full flex items-center gap-2 px-4 py-2.5 text-sm text-slate-600 hover:bg-slate-50 transition-colors">
							<User size={14} />
							Profile
						</button>

						{/* Divider */}
						<div className="border-t border-slate-100 my-1" />

						{/* Logout */}
						<button
							onClick={() => {
								setOpen(false);
								logout();
							}}
							className="w-full flex items-center gap-2 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50 transition-colors">
							<LogOut size={14} />
							Sign out
						</button>
					</div>
				)}
			</div>
		</header>
	);
}
