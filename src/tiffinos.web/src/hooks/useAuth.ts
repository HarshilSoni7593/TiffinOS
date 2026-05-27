import { useRouter } from "next/navigation";
import { useAuthStore } from "../store/authStore";
import api from "../lib/api";
import { AuthResponse, CurrentUser } from "../types/api";
import { jwtDecode } from "jwt-decode";

// Install jwt-decode
// npm install jwt-decode

interface JwtPayload {
	sub: string;
	email: string;
	tenant_id: string;
	first_name: string;
	permission: string[];
	[key: string]: unknown;
}

export function useAuth() {
	const router = useRouter();
	const store = useAuthStore();

	const login = async (email: string, password: string, tenantSlug: string) => {
		const response = await api.post<AuthResponse>(
			"/api/auth/login",
			{ email, password },
			{ headers: { "X-Tenant-Slug": tenantSlug } },
		);

		const { accessToken, refreshToken } = response.data;

		// Decode JWT to get user info
		const decoded = jwtDecode<JwtPayload>(accessToken);

		// Extract roles from JWT
		const roles = Object.entries(decoded)
			.filter(
				([key]) =>
					key ===
					"http://schemas.microsoft.com/ws/2008/06/" + "identity/claims/role",
			)
			.map(([, value]) => value as string);

		const user: CurrentUser = {
			userId: decoded.sub,
			tenantId: decoded.tenant_id,
			email: decoded.email,
			firstName: decoded.first_name,
			roles,
			permissions: decoded.permission ?? [],
		};

		// Store tokens
		localStorage.setItem("accessToken", accessToken);
		localStorage.setItem("refreshToken", refreshToken);
		localStorage.setItem("tenantSlug", tenantSlug);

		// Get tenant name from API
		const tenantResponse = await api.get("/api/auth/me").catch(() => null);
		const tenantName = tenantResponse?.data?.tenantName ?? tenantSlug;

		store.setAuth(user, tenantSlug, tenantName);

		return user;
	};

	const logout = () => {
		const refreshToken = localStorage.getItem("refreshToken");
		if (refreshToken) {
			api.post("/api/auth/logout", { refreshToken }).catch(() => {});
		}
		store.clearAuth();
		router.push("/login");
	};

	return {
		user: store.user,
		tenantSlug: store.tenantSlug,
		tenantName: store.tenantName,
		isAuthenticated: store.isAuthenticated,
		hasPermission: store.hasPermission,
		hasRole: store.hasRole,
		login,
		logout,
	};
}
