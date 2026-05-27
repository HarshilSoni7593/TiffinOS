export default function DashboardPage() {
	return (
		<div className="space-y-6">
			<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
				{[
					"Active Subscriptions",
					"Today's Deliveries",
					"Active Drivers",
					"Monthly Revenue",
				].map((label) => (
					<div
						key={label}
						className="bg-white rounded-xl border border-slate-200 p-5">
						<p className="text-sm text-slate-500">{label}</p>
						<p className="text-2xl font-bold text-slate-800 mt-1">—</p>
					</div>
				))}
			</div>
			<div className="bg-white rounded-xl border border-slate-200 p-6">
				<p className="text-slate-500 text-sm">
					Dashboard summary coming soon. Use the sidebar to manage your tiffin
					operations.
				</p>
			</div>
		</div>
	);
}
