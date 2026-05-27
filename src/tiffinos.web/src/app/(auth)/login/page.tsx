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
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from "@/components/ui/card";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";

const loginSchema = z.object({
	tenantSlug: z.string().min(1, "Restaurant URL is required"),
	email: z.string().email("Invalid email address"),
	password: z.string().min(6, "Password must be at least 6 characters"),
});

type LoginForm = z.infer<typeof loginSchema>;

export default function LoginPage() {
	const router = useRouter();
	const { login } = useAuth();
	const [loading, setLoading] = useState(false);

	const form = useForm<LoginForm>({
		resolver: zodResolver(loginSchema),
		defaultValues: {
			tenantSlug: "",
			email: "",
			password: "",
		},
	});

	const onSubmit = async (data: LoginForm) => {
		setLoading(true);
		try {
			await login(data.email, data.password, data.tenantSlug);
			router.push("/dashboard");
		} catch {
			toast.error("Login failed", {
				description: "Invalid email, password, or restaurant URL.",
			});
		} finally {
			setLoading(false);
		}
	};

	return (
		<Card className="w-full max-w-md">
			<CardHeader className="space-y-1">
				<div className="flex items-center gap-2 mb-2">
					<div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
						<span className="text-white font-bold text-sm">T</span>
					</div>
					<span className="font-semibold text-lg">TiffinOS</span>
				</div>
				<CardTitle className="text-2xl">Sign in</CardTitle>
				<CardDescription>
					Enter your restaurant URL and credentials to access your dashboard
				</CardDescription>
			</CardHeader>
			<CardContent>
				<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
					<div className="space-y-2">
						<Label htmlFor="tenantSlug">Restaurant URL</Label>
						<div className="flex items-center border rounded-md overflow-hidden focus-within:ring-2 focus-within:ring-blue-500">
							<span className="px-3 py-2 bg-slate-50 text-slate-500 text-sm border-r">
								tiffinos.ca/
							</span>
							<Input
								id="tenantSlug"
								placeholder="spicekitchen"
								className="border-0 focus-visible:ring-0 rounded-none"
								{...form.register("tenantSlug")}
							/>
						</div>
						{form.formState.errors.tenantSlug && (
							<p className="text-sm text-red-500">
								{form.formState.errors.tenantSlug.message}
							</p>
						)}
					</div>

					<div className="space-y-2">
						<Label htmlFor="email">Email</Label>
						<Input
							id="email"
							type="email"
							placeholder="admin@spicekitchen.ca"
							{...form.register("email")}
						/>
						{form.formState.errors.email && (
							<p className="text-sm text-red-500">
								{form.formState.errors.email.message}
							</p>
						)}
					</div>

					<div className="space-y-2">
						<Label htmlFor="password">Password</Label>
						<Input
							id="password"
							type="password"
							{...form.register("password")}
						/>
						{form.formState.errors.password && (
							<p className="text-sm text-red-500">
								{form.formState.errors.password.message}
							</p>
						)}
					</div>

					<Button
						type="submit"
						className="w-full bg-blue-600 hover:bg-blue-700"
						disabled={loading}>
						{loading && <Loader2 className="mr-2 animate-spin" />}
						Sign in
					</Button>
				</form>
			</CardContent>
		</Card>
	);
}
