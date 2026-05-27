// ── Auth ──────────────────────────────────────────────────────
export interface LoginRequest {
	email: string;
	password: string;
}

export interface AuthResponse {
	accessToken: string;
	refreshToken: string;
	expiresAt: string;
}

export interface CurrentUser {
	userId: string;
	tenantId: string;
	email: string;
	firstName: string;
	roles: string[];
	permissions: string[];
}

// ── Menu Items ────────────────────────────────────────────────
export interface Category {
	id: string;
	name: string;
	displayOrder: number;
	isActive: boolean;
	itemCount: number;
}

export interface MenuItem {
	id: string;
	categoryId: string | null;
	categoryName: string | null;
	name: string;
	unit: string;
	measurementType: string;
	availablePortions: string[];
	description: string | null;
	isActive: boolean;
	createdAt: string;
}

// ── Tiffin Plans ──────────────────────────────────────────────
export interface PricingTier {
	id: string;
	durationType: string;
	pricePerDay: number;
	billingCycleDays: number;
	totalAmount: number;
	isActive: boolean;
}

export interface PlanItem {
	id: string;
	menuItemId: string;
	menuItemName: string;
	category: string;
	portionSize: string;
	quantity: number;
	displayOrder: number;
}

export interface TiffinPlan {
	id: string;
	name: string;
	description: string | null;
	dietaryType: string;
	boxType: string | null;
	imageUrl: string | null;
	allowSkip: boolean;
	minSkipNoticeHours: number | null;
	maxSkipsPerCycle: number | null;
	skipCreditPolicy: string | null;
	expiryReminderDays: number;
	isActive: boolean;
	pricingTiers: PricingTier[];
	items: PlanItem[];
	createdAt: string;
}

export interface PlanListItem {
	id: string;
	name: string;
	dietaryType: string;
	imageUrl: string | null;
	isActive: boolean;
	tierCount: number;
	itemCount: number;
	lowestPrice: number;
	createdAt: string;
}

// ── Delivery Zones ────────────────────────────────────────────
export interface Zone {
	id: string;
	name: string;
	zoneCode: string;
	colorHex: string | null;
	isActive: boolean;
	activeSubscriptionCount: number;
}

// ── Drivers ───────────────────────────────────────────────────
export interface PayoutPolicy {
	id: string;
	name: string;
	payoutType: string;
	baseRate: number;
	bonusPerDelivery: number | null;
	bonusThreshold: number | null;
	minGuaranteed: number | null;
	currency: string;
	isActive: boolean;
	driverCount: number;
}

export interface Driver {
	id: string;
	userId: string;
	fullName: string;
	vehicleType: string;
	isAvailable: boolean;
	isActive: boolean;
	payoutPolicyName: string | null;
	todayDeliveries: number;
}

// ── Subscriptions ─────────────────────────────────────────────
export interface Subscription {
	id: string;
	customerName: string;
	planName: string;
	durationType: string;
	zoneName: string;
	status: string;
	startDate: string;
	endDate: string | null;
	lockedTotalAmount: number;
	lockedDeliveryCharge: number;
	createdAt: string;
}

// ── Delivery Engine ───────────────────────────────────────────
export interface PackingSummaryItem {
	menuItemId: string;
	itemName: string;
	category: string;
	portionSize: string;
	totalQuantity: number;
	unit: string;
}

export interface PackingSummary {
	date: string;
	totalBoxes: number;
	generatedAt: string;
	items: PackingSummaryItem[];
}

export interface DispatchStop {
	scheduleId: string;
	subscriptionId: string;
	customerName: string;
	customerPhone: string | null;
	deliveryAddress: string;
	floorOrUnit: string | null;
	deliveryInstructions: string | null;
	spicePreference: string | null;
	planName: string;
	lat: number;
	lng: number;
	status: string;
	driverId: string | null;
	sequenceNumber: number | null;
}

export interface DispatchZone {
	zoneId: string;
	zoneName: string;
	zoneCode: string;
	totalDeliveries: number;
	deliveries: DispatchStop[];
}

export interface DispatchList {
	date: string;
	total: number;
	zones: DispatchZone[];
}

// ── Payouts ───────────────────────────────────────────────────
export interface PayoutRecord {
	id: string;
	driverId: string;
	driverName: string;
	policyName: string;
	payoutType: string;
	payoutDate: string;
	totalDeliveries: number;
	totalDistanceKm: number | null;
	totalZones: number | null;
	baseAmount: number;
	bonusAmount: number;
	totalAmount: number;
	status: string;
	notes: string | null;
	createdAt: string;
}

// ── API Response Wrapper ──────────────────────────────────────
export interface ApiError {
	error: string;
	code: string;
}
