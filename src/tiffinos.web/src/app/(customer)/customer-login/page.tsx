"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import Link from "next/link";
import { Loader2 } from "lucide-react";

const loginSchema = z.object({
	email: z.string().email("Invalid email address"),
	password: z.string().min(6, "Password must be at least 6 characters"),
});

type LoginForm = z.infer<typeof loginSchema>;

export default function CustomerLoginPage() {
	const router = useRouter();
	const { login } = useAuth();
	const [loading, setLoading] = useState(false);
	const tenantSlug = process.env.NEXT_PUBLIC_TENANT_SLUG ?? "";

	const form = useForm<LoginForm>({
		resolver: zodResolver(loginSchema),
		defaultValues: { email: "", password: "" },
	});

	const onSubmit = async (data: LoginForm) => {
		setLoading(true);
		try {
			const user = await login(data.email, data.password, tenantSlug);

			// Redirect based on role
			if (user.roles.includes("customer")) {
				router.push("/my-subscription");
			} else {
				// Admin or driver logged in through customer page
				router.push("/dashboard");
			}
		} catch {
			toast.error("Login failed", {
				description: "Invalid email or password.",
			});
		} finally {
			setLoading(false);
		}
	};
	return (
		<div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
			<div className="w-full max-w-sm">
				{/* Logo */}
				<div className="text-center mb-8">
					<Link href="/home">
						<h1 className="text-2xl font-bold text-slate-800 capitalize">
							{tenantSlug}
						</h1>
					</Link>
					<p className="text-slate-500 text-sm mt-1">
						Sign in to manage your subscription
					</p>
				</div>

				{/* Card */}
				<div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
					<h2 className="text-lg font-semibold text-slate-800 mb-6">Sign in</h2>

					<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
						{/* Email */}
						<div className="space-y-2">
							<Label htmlFor="email">Email</Label>
							<Input
								id="email"
								type="email"
								placeholder="you@email.com"
								{...form.register("email")}
							/>
							{form.formState.errors.email && (
								<p className="text-xs text-red-500">
									{form.formState.errors.email.message}
								</p>
							)}
						</div>

						{/* Password */}
						<div className="space-y-2">
							<Label htmlFor="password">Password</Label>
							<Input
								id="password"
								type="password"
								{...form.register("password")}
							/>
							{form.formState.errors.password && (
								<p className="text-xs text-red-500">
									{form.formState.errors.password.message}
								</p>
							)}
						</div>

						{/* Submit */}
						<Button
							type="submit"
							className="w-full bg-blue-600 hover:bg-blue-700"
							disabled={loading}>
							{loading && <Loader2 size={16} className="mr-2 animate-spin" />}
							Sign in
						</Button>
					</form>
				</div>

				{/* Back to home */}
				<p className="text-center text-sm text-slate-400 mt-6">
					<Link href="/home" className="hover:text-slate-600 transition-colors">
						← Back to {tenantSlug}
					</Link>
				</p>
			</div>
		</div>
	);
}
