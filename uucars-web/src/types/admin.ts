export interface DailyCarStat {
  date: string;
  count: number;
}

export interface DailyRevenueStat {
  date: string;
  revenue: number;
}

export interface BrandDistribution {
  brand: string;
  count: number;
}

export interface AdminStats {
  totalCars: number;
  pendingCars: number;
  publishedCars: number;
  totalUsers: number;
  monthlyOrders: number;
  monthlyRevenue: number;
  dailyNewCars: DailyCarStat[];
  dailyRevenue: DailyRevenueStat[];
  brandDistribution: BrandDistribution[];
}

export interface AuditLog {
  id: number;
  adminId: number;
  adminUsername: string;
  action: string;
  entityType: string;
  entityId: number;
  detail: string | null;
  createdAt: string;
}
