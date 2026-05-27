"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import { Category, MenuItem } from "@/types/api";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
	Dialog,
	DialogContent,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import { Plus, Pencil, Trash2, ChevronDown, ChevronUp } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { cn } from "@/lib/utils";

// ── Schemas ───────────────────────────────────────────────────
const categorySchema = z.object({
	name: z.string().min(1, "Name is required"),
	displayOrder: z.coerce.number().min(0),
});

const menuItemSchema = z.object({
	name: z.string().min(1, "Name is required"),
	categoryId: z.string().min(1, "Category is required"),
	unit: z.string().min(1, "Unit is required"),
	measurementType: z.string().min(1, "Measurement type is required"),
	description: z.string().optional(),
	portions: z.string().min(1, "At least one portion size is required"),
});

type CategoryForm = z.infer<typeof categorySchema>;
type MenuItemForm = z.infer<typeof menuItemSchema>;

// ── Main Page ─────────────────────────────────────────────────
export default function MenuItemsPage() {
	const queryClient = useQueryClient();
	const [activeTab, setActiveTab] = useState<"items" | "categories">("items");
	const [categoryDialogOpen, setCategoryDialog] = useState(false);
	const [itemDialogOpen, setItemDialog] = useState(false);
	const [editingCategory, setEditingCategory] = useState<Category | null>(null);
	const [editingItem, setEditingItem] = useState<MenuItem | null>(null);
	const [expandedCategories, setExpandedCategories] = useState<Set<string>>(
		new Set(),
	);

	// ── Queries ───────────────────────────────────────────────
	const { data: categories = [], isLoading: loadingCategories } = useQuery<
		Category[]
	>({
		queryKey: ["categories"],
		queryFn: () => api.get("/api/menu-items/categories").then((r) => r.data),
	});

	const { data: menuItems = [], isLoading: loadingItems } = useQuery<
		MenuItem[]
	>({
		queryKey: ["menuItems"],
		queryFn: () => api.get("/api/menu-items").then((r) => r.data),
	});

	// ── Mutations ─────────────────────────────────────────────
	const createCategory = useMutation({
		mutationFn: (data: CategoryForm) =>
			api.post("/api/menu-items/categories", data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["categories"] });
			setCategoryDialog(false);
			toast.success("Category created successfully");
		},
		onError: () => toast.error("Failed to create category"),
	});

	const updateCategory = useMutation({
		mutationFn: (data: CategoryForm & { id: string }) =>
			api.put(`/api/menu-items/categories/${data.id}`, data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["categories"] });
			setCategoryDialog(false);
			setEditingCategory(null);
			toast.success("Category updated successfully");
		},
		onError: () => toast.error("Failed to update category"),
	});

	const createMenuItem = useMutation({
		mutationFn: (data: MenuItemForm) =>
			api.post("/api/menu-items", {
				name: data.name,
				categoryId: data.categoryId,
				unit: data.unit,
				measurementType: data.measurementType,
				description: data.description || null,
				availablePortions: data.portions
					.split(",")
					.map((p) => p.trim())
					.filter(Boolean),
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["menuItems"] });
			setItemDialog(false);
			toast.success("Menu item created successfully");
		},
		onError: () => toast.error("Failed to create menu item"),
	});

	const updateMenuItem = useMutation({
		mutationFn: (data: MenuItemForm & { id: string }) =>
			api.put(`/api/menu-items/${data.id}`, {
				name: data.name,
				categoryId: data.categoryId,
				unit: data.unit,
				measurementType: data.measurementType,
				description: data.description || null,
				availablePortions: data.portions
					.split(",")
					.map((p) => p.trim())
					.filter(Boolean),
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["menuItems"] });
			setItemDialog(false);
			setEditingItem(null);
			toast.success("Menu item updated successfully");
		},
		onError: () => toast.error("Failed to update menu item"),
	});

	const deactivateItem = useMutation({
		mutationFn: (id: string) => api.delete(`/api/menu-items/${id}`),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ["menuItems"] });
			toast.success("Menu item deactivated");
		},
		onError: (e: any) =>
			toast.error(e?.response?.data?.error ?? "Failed to deactivate item"),
	});

	// ── Helpers ───────────────────────────────────────────────
	const openCreateCategory = () => {
		setEditingCategory(null);
		setCategoryDialog(true);
	};

	const openEditCategory = (cat: Category) => {
		setEditingCategory(cat);
		setCategoryDialog(true);
	};

	const openCreateItem = () => {
		setEditingItem(null);
		setItemDialog(true);
	};

	const openEditItem = (item: MenuItem) => {
		setEditingItem(item);
		setItemDialog(true);
	};

	const toggleCategory = (id: string) => {
		setExpandedCategories((prev) => {
			const next = new Set(prev);
			next.has(id) ? next.delete(id) : next.add(id);
			return next;
		});
	};

	// Group items by category
	const itemsByCategory = categories.map((cat) => ({
		category: cat,
		items: menuItems.filter((i) => i.categoryId === cat.id),
	}));

	const uncategorised = menuItems.filter((i) => !i.categoryId);

	return (
		<div className="space-y-6">
			{/* Page Actions */}
			<div className="flex items-center justify-between">
				<div className="flex gap-2">
					<Button
						variant={activeTab === "items" ? "default" : "outline"}
						size="sm"
						onClick={() => setActiveTab("items")}>
						Menu Items ({menuItems.length})
					</Button>
					<Button
						variant={activeTab === "categories" ? "default" : "outline"}
						size="sm"
						onClick={() => setActiveTab("categories")}>
						Categories ({categories.length})
					</Button>
				</div>
				<Button
					size="sm"
					className="bg-blue-600 hover:bg-blue-700"
					onClick={activeTab === "items" ? openCreateItem : openCreateCategory}>
					<Plus size={16} className="mr-2" />
					{activeTab === "items" ? "Add Item" : "Add Category"}
				</Button>
			</div>

			{/* Menu Items Tab */}
			{activeTab === "items" && (
				<div className="space-y-3">
					{loadingItems ? (
						Array.from({ length: 3 }).map((_, i) => (
							<Skeleton key={i} className="h-16 w-full rounded-xl" />
						))
					) : (
						<>
							{itemsByCategory.map(({ category, items }) => (
								<div
									key={category.id}
									className="bg-white rounded-xl border border-slate-200 overflow-hidden">
									{/* Category Header */}
									<button
										onClick={() => toggleCategory(category.id)}
										className="w-full flex items-center justify-between px-5 py-4 hover:bg-slate-50 transition-colors">
										<div className="flex items-center gap-3">
											<span className="font-medium text-slate-800">
												{category.name}
											</span>
											<Badge variant="secondary">{items.length} items</Badge>
										</div>
										{expandedCategories.has(category.id) ? (
											<ChevronUp size={16} className="text-slate-400" />
										) : (
											<ChevronDown size={16} className="text-slate-400" />
										)}
									</button>

									{/* Items List */}
									{expandedCategories.has(category.id) && (
										<div className="border-t border-slate-100">
											{items.length === 0 ? (
												<p className="text-slate-400 text-sm px-5 py-4">
													No items in this category yet.
												</p>
											) : (
												items.map((item) => (
													<div
														key={item.id}
														className="flex items-center justify-between px-5 py-3 border-b border-slate-50 last:border-0 hover:bg-slate-50">
														<div className="flex items-center gap-4">
															<div>
																<p className="text-sm font-medium text-slate-800">
																	{item.name}
																</p>
																<p className="text-xs text-slate-400">
																	{item.unit} · {item.measurementType} ·{" "}
																	{item.availablePortions.join(", ")}
																</p>
															</div>
														</div>
														<div className="flex items-center gap-2">
															<Badge
																variant={
																	item.isActive ? "default" : "secondary"
																}
																className={cn(
																	"text-xs",
																	item.isActive
																		? "bg-green-100 text-green-700 hover:bg-green-100"
																		: "",
																)}>
																{item.isActive ? "Active" : "Inactive"}
															</Badge>
															<Button
																variant="ghost"
																size="icon"
																className="h-8 w-8"
																onClick={() => openEditItem(item)}>
																<Pencil size={14} />
															</Button>
															{item.isActive && (
																<Button
																	variant="ghost"
																	size="icon"
																	className="h-8 w-8 text-red-500 hover:text-red-600 hover:bg-red-50"
																	onClick={() =>
																		deactivateItem.mutate(item.id)
																	}>
																	<Trash2 size={14} />
																</Button>
															)}
														</div>
													</div>
												))
											)}
										</div>
									)}
								</div>
							))}

							{/* Uncategorised */}
							{uncategorised.length > 0 && (
								<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
									<div className="px-5 py-4 border-b border-slate-100">
										<span className="font-medium text-slate-500">
											Uncategorised ({uncategorised.length})
										</span>
									</div>
									{uncategorised.map((item) => (
										<div
											key={item.id}
											className="flex items-center justify-between px-5 py-3 border-b border-slate-50 last:border-0">
											<p className="text-sm font-medium">{item.name}</p>
											<Button
												variant="ghost"
												size="icon"
												className="h-8 w-8"
												onClick={() => openEditItem(item)}>
												<Pencil size={14} />
											</Button>
										</div>
									))}
								</div>
							)}

							{menuItems.length === 0 && (
								<div className="bg-white rounded-xl border border-slate-200 p-12 text-center">
									<p className="text-slate-400 text-sm">
										No menu items yet. Add categories first, then add items.
									</p>
									<Button
										size="sm"
										className="mt-4 bg-blue-600 hover:bg-blue-700"
										onClick={openCreateItem}>
										<Plus size={16} className="mr-2" />
										Add First Item
									</Button>
								</div>
							)}
						</>
					)}
				</div>
			)}

			{/* Categories Tab */}
			{activeTab === "categories" && (
				<div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
					{loadingCategories ? (
						<div className="p-4 space-y-3">
							{Array.from({ length: 3 }).map((_, i) => (
								<Skeleton key={i} className="h-12 w-full" />
							))}
						</div>
					) : categories.length === 0 ? (
						<div className="p-12 text-center">
							<p className="text-slate-400 text-sm">
								No categories yet. Create your first category.
							</p>
							<Button
								size="sm"
								className="mt-4 bg-blue-600 hover:bg-blue-700"
								onClick={openCreateCategory}>
								<Plus size={16} className="mr-2" />
								Add Category
							</Button>
						</div>
					) : (
						<table className="w-full">
							<thead>
								<tr className="border-b border-slate-100 bg-slate-50">
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Name
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Display Order
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Items
									</th>
									<th className="text-left text-xs font-medium text-slate-500 px-5 py-3">
										Status
									</th>
									<th className="px-5 py-3" />
								</tr>
							</thead>
							<tbody>
								{categories.map((cat) => (
									<tr
										key={cat.id}
										className="border-b border-slate-50 last:border-0 hover:bg-slate-50">
										<td className="px-5 py-3 text-sm font-medium text-slate-800">
											{cat.name}
										</td>
										<td className="px-5 py-3 text-sm text-slate-500">
											{cat.displayOrder}
										</td>
										<td className="px-5 py-3 text-sm text-slate-500">
											{cat.itemCount}
										</td>
										<td className="px-5 py-3">
											<Badge
												variant="secondary"
												className="bg-green-100 text-green-700 hover:bg-green-100 text-xs">
												Active
											</Badge>
										</td>
										<td className="px-5 py-3 text-right">
											<Button
												variant="ghost"
												size="icon"
												className="h-8 w-8"
												onClick={() => openEditCategory(cat)}>
												<Pencil size={14} />
											</Button>
										</td>
									</tr>
								))}
							</tbody>
						</table>
					)}
				</div>
			)}

			{/* Category Dialog */}
			<CategoryDialog
				open={categoryDialogOpen}
				onClose={() => {
					setCategoryDialog(false);
					setEditingCategory(null);
				}}
				editing={editingCategory}
				onSubmit={(data) => {
					if (editingCategory)
						updateCategory.mutate({ ...data, id: editingCategory.id });
					else createCategory.mutate(data);
				}}
				loading={createCategory.isPending || updateCategory.isPending}
			/>

			{/* Menu Item Dialog */}
			<MenuItemDialog
				open={itemDialogOpen}
				onClose={() => {
					setItemDialog(false);
					setEditingItem(null);
				}}
				editing={editingItem}
				categories={categories}
				onSubmit={(data) => {
					if (editingItem)
						updateMenuItem.mutate({ ...data, id: editingItem.id });
					else createMenuItem.mutate(data);
				}}
				loading={createMenuItem.isPending || updateMenuItem.isPending}
			/>
		</div>
	);
}

// ── Category Dialog ───────────────────────────────────────────
function CategoryDialog({
	open,
	onClose,
	editing,
	onSubmit,
	loading,
}: {
	open: boolean;
	onClose: () => void;
	editing: Category | null;
	onSubmit: (data: CategoryForm) => void;
	loading: boolean;
}) {
	const form = useForm<CategoryForm>({
		resolver: zodResolver(categorySchema),
		values: editing
			? { name: editing.name, displayOrder: editing.displayOrder }
			: { name: "", displayOrder: 0 },
	});

	return (
		<Dialog open={open} onOpenChange={onClose}>
			<DialogContent className="sm:max-w-md">
				<DialogHeader>
					<DialogTitle>
						{editing ? "Edit Category" : "Add Category"}
					</DialogTitle>
				</DialogHeader>
				<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 pt-2">
					<div className="space-y-2">
						<Label>Category Name</Label>
						<Input
							placeholder="e.g. Bread, Curry, Rice"
							{...form.register("name")}
						/>
						{form.formState.errors.name && (
							<p className="text-xs text-red-500">
								{form.formState.errors.name.message}
							</p>
						)}
					</div>
					<div className="space-y-2">
						<Label>Display Order</Label>
						<Input
							type="number"
							placeholder="1"
							{...form.register("displayOrder")}
						/>
						<p className="text-xs text-slate-400">
							Lower number appears first in the packing summary
						</p>
					</div>
					<div className="flex justify-end gap-2 pt-2">
						<Button type="button" variant="outline" onClick={onClose}>
							Cancel
						</Button>
						<Button
							type="submit"
							className="bg-blue-600 hover:bg-blue-700"
							disabled={loading}>
							{loading ? "Saving..." : editing ? "Save Changes" : "Create"}
						</Button>
					</div>
				</form>
			</DialogContent>
		</Dialog>
	);
}

// ── Menu Item Dialog ──────────────────────────────────────────
function MenuItemDialog({
	open,
	onClose,
	editing,
	categories,
	onSubmit,
	loading,
}: {
	open: boolean;
	onClose: () => void;
	editing: MenuItem | null;
	categories: Category[];
	onSubmit: (data: MenuItemForm) => void;
	loading: boolean;
}) {
	const form = useForm<MenuItemForm>({
		resolver: zodResolver(menuItemSchema),
		values: editing
			? {
					name: editing.name,
					categoryId: editing.categoryId ?? "",
					unit: editing.unit,
					measurementType: editing.measurementType,
					description: editing.description ?? "",
					portions: editing.availablePortions.join(", "),
				}
			: {
					name: "",
					categoryId: "",
					unit: "",
					measurementType: "",
					description: "",
					portions: "",
				},
	});

	return (
		<Dialog open={open} onOpenChange={onClose}>
			<DialogContent className="sm:max-w-lg">
				<DialogHeader>
					<DialogTitle>
						{editing ? "Edit Menu Item" : "Add Menu Item"}
					</DialogTitle>
				</DialogHeader>
				<form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 pt-2">
					<div className="space-y-2">
						<Label>Item Name</Label>
						<Input
							placeholder="e.g. Chapati, Dal Tadka"
							{...form.register("name")}
						/>
						{form.formState.errors.name && (
							<p className="text-xs text-red-500">
								{form.formState.errors.name.message}
							</p>
						)}
					</div>

					<div className="space-y-2">
						<Label>Category</Label>
						<Select
							value={form.watch("categoryId")}
							onValueChange={(v) => form.setValue("categoryId", v)}>
							<SelectTrigger>
								<SelectValue placeholder="Select a category" />
							</SelectTrigger>
							<SelectContent>
								{categories.map((cat) => (
									<SelectItem key={cat.id} value={cat.id}>
										{cat.name}
									</SelectItem>
								))}
							</SelectContent>
						</Select>
						{form.formState.errors.categoryId && (
							<p className="text-xs text-red-500">
								{form.formState.errors.categoryId.message}
							</p>
						)}
					</div>

					<div className="grid grid-cols-2 gap-4">
						<div className="space-y-2">
							<Label>Unit</Label>
							<Select
								value={form.watch("unit")}
								onValueChange={(v) => form.setValue("unit", v)}>
								<SelectTrigger>
									<SelectValue placeholder="Select unit" />
								</SelectTrigger>
								<SelectContent>
									{["piece", "portion", "bowl", "glass", "pack"].map((u) => (
										<SelectItem key={u} value={u}>
											{u}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
						<div className="space-y-2">
							<Label>Measurement Type</Label>
							<Select
								value={form.watch("measurementType")}
								onValueChange={(v) => form.setValue("measurementType", v)}>
								<SelectTrigger>
									<SelectValue placeholder="Select type" />
								</SelectTrigger>
								<SelectContent>
									{["volume", "weight", "pack", "count"].map((t) => (
										<SelectItem key={t} value={t}>
											{t}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</div>
					</div>

					<div className="space-y-2">
						<Label>Available Portions</Label>
						<Input
							placeholder="8oz, 12oz, 16oz  or  Pack of 4, Pack of 6"
							{...form.register("portions")}
						/>
						<p className="text-xs text-slate-400">
							Separate multiple portions with a comma
						</p>
						{form.formState.errors.portions && (
							<p className="text-xs text-red-500">
								{form.formState.errors.portions.message}
							</p>
						)}
					</div>

					<div className="space-y-2">
						<Label>Description (optional)</Label>
						<Input
							placeholder="Brief description"
							{...form.register("description")}
						/>
					</div>

					<div className="flex justify-end gap-2 pt-2">
						<Button type="button" variant="outline" onClick={onClose}>
							Cancel
						</Button>
						<Button
							type="submit"
							className="bg-blue-600 hover:bg-blue-700"
							disabled={loading}>
							{loading ? "Saving..." : editing ? "Save Changes" : "Create Item"}
						</Button>
					</div>
				</form>
			</DialogContent>
		</Dialog>
	);
}
