"use client";

import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { useAuthStore } from "@/store/authStore";
import { Button } from "@/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { LogOut, User } from "lucide-react";

// Maps route paths to readable page titles
const pageTitles: Record<string, string> = {
	"/dashboard": "Dashboard",
	"/menu-items": "Menu Items",
	"/plans": "Tiffin Plans",
	"/zones": "Delivery Zones",
	"/subscriptions": "Subscriptions",
	"/drivers": "Drivers",
	"/routes": "Routes",
	"/payouts": "Payouts",
};

export default function Header() {
	const pathname = usePathname();
	const { logout } = useAuth();
	const user = useAuthStore((s) => s.user);

	const title =
		pageTitles[pathname] ??
		pageTitles[
			Object.keys(pageTitles).find((k) => pathname.startsWith(k)) ?? ""
		] ??
		"TiffinOS";

	return (
		<header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6 flex-shrink-0">
			{/* Page title */}
			<h1 className="text-lg font-semibold text-slate-800">{title}</h1>

			{/* Right side */}
			<div className="flex items-center gap-3">
				{/* User menu */}
				<DropdownMenu>
					<DropdownMenuTrigger className="flex items-center gap-2 h-9 px-3 rounded-lg hover:bg-slate-100 transition-colors outline-none">
						<div className="w-7 h-7 rounded-full bg-blue-600 flex items-center justify-center">
							<span className="text-white text-xs font-semibold">
								{user?.firstName?.charAt(0).toUpperCase()}
							</span>
						</div>
						<span className="text-sm font-medium text-slate-700">
							{user?.firstName}
						</span>
					</DropdownMenuTrigger>
					<DropdownMenuContent align="end" className="w-48">
						<DropdownMenuLabel>
							<p className="text-sm font-medium">{user?.firstName}</p>
							<p className="text-xs text-slate-500 font-normal">
								{user?.email}
							</p>
						</DropdownMenuLabel>
						<DropdownMenuSeparator />
						<DropdownMenuItem className="text-slate-600">
							<User size={14} className="mr-2" />
							Profile
						</DropdownMenuItem>
						<DropdownMenuSeparator />
						<DropdownMenuItem
							className="text-red-600 focus:text-red-600"
							onClick={logout}>
							<LogOut size={14} className="mr-2" />
							Sign out
						</DropdownMenuItem>
					</DropdownMenuContent>
				</DropdownMenu>
			</div>
		</header>
	);
}
