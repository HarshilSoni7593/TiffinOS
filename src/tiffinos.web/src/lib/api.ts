import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

export const api = axios.create({
	baseURL: API_URL,
	headers: {
		"Content-Type": "application/json",
	},
});

// ── Request Interceptor ───────────────────────────────────────
// Automatically attaches JWT token and tenant slug
// to every outgoing request
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
	if (typeof window !== "undefined") {
		const token = localStorage.getItem("accessToken");
		const tenantSlug = localStorage.getItem("tenantSlug");

		if (token) config.headers.Authorization = `Bearer ${token}`;

		if (tenantSlug) config.headers["X-Tenant-Slug"] = tenantSlug;
	}

	return config;
});

// ── Response Interceptor ──────────────────────────────────────
// Handles token refresh when access token expires.
// If refresh fails, clears auth and redirects to login.
api.interceptors.response.use(
	(response) => response,
	async (error: AxiosError) => {
		const original = error.config as InternalAxiosRequestConfig & {
			_retry?: boolean;
		};

		if (error.response?.status === 401 && !original._retry) {
			original._retry = true;

			try {
				const refreshToken = localStorage.getItem("refreshToken");
				if (!refreshToken) throw new Error("No refresh token");

				const response = await axios.post(`${API_URL}/api/auth/refresh`, {
					refreshToken,
				});

				const { accessToken, refreshToken: newRefresh } = response.data;

				localStorage.setItem("accessToken", accessToken);
				localStorage.setItem("refreshToken", newRefresh);

				original.headers.Authorization = `Bearer ${accessToken}`;
				return api(original);
			} catch {
				// Refresh failed — clear everything and redirect to login
				localStorage.clear();
				window.location.href = "/login";
			}
		}

		return Promise.reject(error);
	},
);

export default api;
