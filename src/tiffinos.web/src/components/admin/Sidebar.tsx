"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuthStore } from "@/store/authStore";
import { cn } from "@/lib/utils";
import {
	LayoutDashboard,
	UtensilsCrossed,
	ClipboardList,
	MapPin,
	Users,
	Route,
	Wallet,
	Package,
	ChevronRight,
} from "lucide-react";

const navigation = [
	{
		section: "Overview",
		items: [
			{
				label: "Dashboard",
				href: "/dashboard",
				icon: LayoutDashboard,
				permission: null,
			},
		],
	},
	{
		section: "Tiffin Setup",
		items: [
			{
				label: "Menu Items",
				href: "/menu-items",
				icon: UtensilsCrossed,
				permission: "tiffin:plans:read",
			},
			{
				label: "Tiffin Plans",
				href: "/plans",
				icon: ClipboardList,
				permission: "tiffin:plans:read",
			},
			{
				label: "Delivery Zones",
				href: "/zones",
				icon: MapPin,
				permission: "tiffin:plans:read",
			},
		],
	},
	{
		section: "Operations",
		items: [
			{
				label: "Subscriptions",
				href: "/subscriptions",
				icon: Package,
				permission: "tiffin:subscriptions:read:all",
			},
			{
				label: "Drivers",
				href: "/drivers",
				icon: Users,
				permission: "tiffin:drivers:manage",
			},
			{
				label: "Routes",
				href: "/routes",
				icon: Route,
				permission: "tiffin:routes:view",
			},
			{
				label: "Packing Summary",
				href: "/packing",
				icon: Package,
				permission: "tiffin:packing:summary:view",
			},
			{
				label: "Payouts",
				href: "/payouts",
				icon: Wallet,
				permission: "tiffin:drivers:manage",
			},
		],
	},
];

export default function Sidebar() {
	const pathname = usePathname();
	const hasPermission = useAuthStore((s) => s.hasPermission);
	const tenantName = useAuthStore((s) => s.tenantName);

	return (
		<aside className="w-64 bg-slate-900 text-white flex flex-col h-full flex-shrink-0">
			{/* Logo */}
			<div className="flex items-center gap-3 px-6 py-5 border-b border-slate-700">
				<div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center flex-shrink-0">
					<span className="text-white font-bold text-sm">T</span>
				</div>
				<div className="overflow-hidden">
					<p className="font-semibold text-sm leading-tight">TiffinOS</p>
					<p className="text-slate-400 text-xs truncate">{tenantName}</p>
				</div>
			</div>

			{/* Navigation */}
			<nav className="flex-1 overflow-y-auto px-3 py-4 space-y-6">
				{navigation.map((group) => (
					<div key={group.section}>
						<p className="px-3 text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">
							{group.section}
						</p>
						<ul className="space-y-1">
							{group.items.map((item) => {
								// Hide items the user doesn't have permission for
								if (item.permission && !hasPermission(item.permission))
									return null;

								const isActive =
									pathname === item.href ||
									pathname.startsWith(item.href + "/");

								return (
									<li key={item.href}>
										<Link
											href={item.href}
											className={cn(
												"flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors",
												isActive
													? "bg-blue-600 text-white"
													: "text-slate-400 hover:bg-slate-800 hover:text-white",
											)}>
											<item.icon size={18} />
											<span className="flex-1">{item.label}</span>
											{isActive && (
												<ChevronRight size={14} className="opacity-60" />
											)}
										</Link>
									</li>
								);
							})}
						</ul>
					</div>
				))}
			</nav>

			{/* Version */}
			<div className="px-6 py-4 border-t border-slate-700">
				<p className="text-slate-500 text-xs">TiffinOS v1.0</p>
			</div>
		</aside>
	);
}
