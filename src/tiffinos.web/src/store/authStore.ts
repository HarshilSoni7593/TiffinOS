import { create } from "zustand";
import { persist } from "zustand/middleware";
import { CurrentUser } from "../types/api";

interface AuthState {
	user: CurrentUser | null;
	tenantSlug: string | null;
	tenantName: string | null;
	isAuthenticated: boolean;

	setAuth: (user: CurrentUser, tenantSlug: string, tenantName: string) => void;
	clearAuth: () => void;
	hasPermission: (permission: string) => boolean;
	hasRole: (role: string) => boolean;
}

export const useAuthStore = create<AuthState>()(
	persist(
		(set, get) => ({
			user: null,
			tenantSlug: null,
			tenantName: null,
			isAuthenticated: false,

			setAuth: (user, tenantSlug, tenantName) => {
				localStorage.setItem("tenantSlug", tenantSlug);
				set({ user, tenantSlug, tenantName, isAuthenticated: true });
			},

			clearAuth: () => {
				localStorage.clear();
				set({
					user: null,
					tenantSlug: null,
					tenantName: null,
					isAuthenticated: false,
				});
			},

			hasPermission: (permission) => {
				const { user } = get();
				return user?.permissions.includes(permission) ?? false;
			},

			hasRole: (role) => {
				const { user } = get();
				return user?.roles.includes(role) ?? false;
			},
		}),
		{
			name: "tiffinos-auth",
			// Only persist non-sensitive state
			// Tokens stay in localStorage separately
			partialize: (state) => ({
				user: state.user,
				tenantSlug: state.tenantSlug,
				tenantName: state.tenantName,
				isAuthenticated: state.isAuthenticated,
			}),
		},
	),
);
