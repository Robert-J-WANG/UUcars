import { adminApi } from "@/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { Skeleton } from "@/components/ui/skeleton";
import { useQuery } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import {
  Car,
  CheckCircle,
  Clock,
  DollarSign,
  ShoppingCart,
  Users,
  FileClock,
} from "lucide-react";
import EmptyState from "@/components/EmptyState";
import Pagination from "@/components/Pagination";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const PAGE_SIZE = 10;

// ── 格式化辅助方法 ─────────────────────────────────────────
// 普通数字：使用新西兰千分位格式
const formatNumber = (value: number) => value.toLocaleString("en-NZ");

// 金额：使用新西兰元格式
const formatCurrency = (value: number) =>
  value.toLocaleString("en-NZ", {
    style: "currency",
    currency: "NZD",
  });

// 趋势图日期：将 yyyy-MM-dd 显示为 dd/MM
const formatChartDate = (value: string) => {
  const [, month, day] = value.split("-");
  return `${day}/${month}`;
};

// 审计日志时间：将 UTC 时间转换为用户本地时间，并使用新西兰格式
const formatAuditDate = (value: string) => {
  const utcValue = value.endsWith("Z") ? value : `${value}Z`;

  return new Date(utcValue).toLocaleString("en-NZ", {
    dateStyle: "medium",
    timeStyle: "short",
  });
};

function AdminDashboardPage() {
  const [searchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  // ── 拉取统计数据 ─────────────────────────────────────────
  const {
    data: stats,
    isLoading: statsIsLoading,
    error: statsError,
  } = useQuery({
    queryKey: ["admin", "stats"],
    queryFn: () => adminApi.getStats(),
  });

  // ── 拉取审计日志 ─────────────────────────────────────────
  const {
    data: auditLogs,
    isLoading: auditLogsIsLoading,
    error: auditLogsError,
  } = useQuery({
    queryKey: ["admin", "audit-logs", { page, pageSize: PAGE_SIZE }],
    queryFn: () => adminApi.getAuditLogs(page, PAGE_SIZE),
  });
  // 统计卡片数据数组
  const statCards = stats
    ? [
        {
          title: "Total cars",
          value: formatNumber(stats.totalCars),
          icon: Car,
        },
        {
          title: "Pending cars",
          value: formatNumber(stats.pendingCars),
          icon: Clock,
        },
        {
          title: "Published cars",
          value: formatNumber(stats.publishedCars),
          icon: CheckCircle,
        },
        {
          title: "Total users",
          value: formatNumber(stats.totalUsers),
          icon: Users,
        },
        {
          title: "Orders this month",
          value: formatNumber(stats.monthlyOrders),
          icon: ShoppingCart,
        },
        {
          title: "Revenue this month",
          value: formatCurrency(stats.monthlyRevenue),
          icon: DollarSign,
        },
      ]
    : [];

  // 定义饼图颜色
  const PIE_COLORS = [
    "#c0392b",
    "#1c1c1e",
    "#d97706",
    "#2563eb",
    "#16a34a",
    "#7c3aed",
    "#0891b2",
    "#db2777",
    "#65a30d",
    "#64748b",
  ];
  // 饼图数据
  const brandChartData =
    stats?.brandDistribution.map((item, index) => ({
      ...item,
      fill: PIE_COLORS[index % PIE_COLORS.length],
    })) ?? [];

  return (
    <div className="space-y-6">
      {/* Stats Loading */}
      {statsIsLoading && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-28" />
          ))}
        </div>
      )}

      {/* Stats Error */}
      {statsError && (
        <div
          className="rounded-[var(--radius-lg)] border px-6 py-10 text-center text-sm"
          style={{
            color: "var(--color-danger)",
            borderColor: "var(--color-border)",
            backgroundColor: "var(--color-surface)",
          }}
        >
          Failed to load dashboard statistics. Please try again.
        </div>
      )}

      {/* Stats Success */}
      {stats && (
        <>
          {/* 统计卡片 */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {statCards.map((item) => {
              const Icon = item.icon;

              return (
                <Card key={item.title}>
                  <CardContent className="flex items-center justify-between pt-5">
                    <div>
                      <p
                        className="text-sm"
                        style={{ color: "var(--color-text-secondary)" }}
                      >
                        {item.title}
                      </p>

                      <p
                        className="mt-2 text-2xl font-semibold"
                        style={{ color: "var(--color-text-primary)" }}
                      >
                        {item.value}
                      </p>
                    </div>

                    <div
                      className="flex h-11 w-11 items-center justify-center rounded-full"
                      style={{
                        color: "var(--color-accent)",
                        backgroundColor: "var(--color-accent-light)",
                      }}
                    >
                      <Icon className="h-5 w-5" aria-hidden="true" />
                    </div>
                  </CardContent>
                </Card>
              );
            })}
          </div>

          <div className="grid grid-cols-1 gap-4 xl:grid-cols-2">
            {/* 趋势折线图：每日新增车辆 */}
            <Card>
              <CardHeader>
                <CardTitle>Daily new cars</CardTitle>
                <CardDescription>
                  New cars added during the last 30 days
                </CardDescription>
              </CardHeader>

              <CardContent>
                <div
                  className="h-72 w-full"
                  role="img"
                  aria-label="Daily new cars during the last 30 days"
                >
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart
                      data={stats.dailyNewCars}
                      margin={{ top: 5, right: 10, left: 0, bottom: 0 }}
                      accessibilityLayer
                    >
                      <CartesianGrid
                        stroke="var(--color-border)"
                        strokeDasharray="3 3"
                      />

                      <XAxis
                        dataKey="date"
                        tickFormatter={formatChartDate}
                        tick={{
                          fill: "var(--color-text-secondary)",
                          fontSize: 12,
                        }}
                      />

                      <YAxis
                        allowDecimals={false}
                        width={50}
                        tick={{
                          fill: "var(--color-text-secondary)",
                          fontSize: 12,
                        }}
                      />

                      <Tooltip
                        labelFormatter={(label) =>
                          formatChartDate(String(label))
                        }
                        formatter={(value) => [
                          formatNumber(Number(value)),
                          "New cars",
                        ]}
                        contentStyle={{
                          backgroundColor: "var(--color-surface)",
                          borderColor: "var(--color-border)",
                          borderRadius: "var(--radius-md)",
                        }}
                      />

                      <Line
                        type="linear"
                        dataKey="count"
                        stroke="var(--color-accent)"
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>

            {/* 趋势折线图：每日成交额 */}
            <Card>
              <CardHeader>
                <CardTitle>Daily revenue</CardTitle>
                <CardDescription>
                  Completed order revenue during the last 30 days
                </CardDescription>
              </CardHeader>

              <CardContent>
                <div
                  className="h-72 w-full"
                  role="img"
                  aria-label="Daily revenue during the last 30 days"
                >
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart
                      data={stats.dailyRevenue}
                      margin={{ top: 5, right: 10, left: 0, bottom: 0 }}
                      accessibilityLayer
                    >
                      <CartesianGrid
                        stroke="var(--color-border)"
                        strokeDasharray="3 3"
                      />

                      <XAxis
                        dataKey="date"
                        tickFormatter={formatChartDate}
                        tick={{
                          fill: "var(--color-text-secondary)",
                          fontSize: 12,
                        }}
                      />

                      <YAxis
                        width={70}
                        tickFormatter={(value) => formatNumber(Number(value))}
                        tick={{
                          fill: "var(--color-text-secondary)",
                          fontSize: 12,
                        }}
                      />

                      <Tooltip
                        labelFormatter={(label) =>
                          formatChartDate(String(label))
                        }
                        formatter={(value) => [
                          formatCurrency(Number(value)),
                          "Revenue",
                        ]}
                        contentStyle={{
                          backgroundColor: "var(--color-surface)",
                          borderColor: "var(--color-border)",
                          borderRadius: "var(--radius-md)",
                        }}
                      />

                      <Line
                        type="linear"
                        dataKey="revenue"
                        stroke="var(--color-primary)"
                        strokeWidth={2}
                        dot={false}
                        activeDot={{ r: 4 }}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* 饼图：车辆品牌贡献 */}
          <Card>
            <CardHeader>
              <CardTitle>Brand distribution</CardTitle>
              <CardDescription>
                Top 10 brands by number of active listings
              </CardDescription>
            </CardHeader>

            <CardContent>
              {brandChartData.length === 0 ? (
                <p
                  className="py-16 text-center text-sm"
                  style={{ color: "var(--color-text-secondary)" }}
                >
                  No brand data available.
                </p>
              ) : (
                <div
                  className="h-80 w-full"
                  role="img"
                  aria-label="Top 10 car brands by number of active listings"
                >
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart accessibilityLayer>
                      <Pie
                        data={brandChartData}
                        dataKey="count"
                        nameKey="brand"
                        cx="50%"
                        cy="50%"
                        innerRadius={55}
                        outerRadius={105}
                        paddingAngle={2}
                      />

                      <Tooltip
                        formatter={(value) => formatNumber(Number(value))}
                        contentStyle={{
                          backgroundColor: "var(--color-surface)",
                          borderColor: "var(--color-border)",
                          borderRadius: "var(--radius-md)",
                        }}
                      />

                      <Legend />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              )}
            </CardContent>
          </Card>
        </>
      )}

      {/* Audit Logs 独立区域 */}
      <Card>
        <CardHeader>
          <CardTitle>Audit logs</CardTitle>
          <CardDescription>
            Recent administrative activity on the platform
          </CardDescription>
        </CardHeader>

        <CardContent>
          {/* Audit Loading */}
          {auditLogsIsLoading && (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <Skeleton key={index} className="h-10" />
              ))}
            </div>
          )}

          {/* Audit Error */}
          {auditLogsError && (
            <div
              className="py-10 text-center text-sm"
              style={{ color: "var(--color-danger)" }}
            >
              Failed to load audit logs. Please try again.
            </div>
          )}

          {/* Audit Empty */}
          {!auditLogsIsLoading &&
            !auditLogsError &&
            auditLogs &&
            auditLogs.items.length === 0 && (
              <EmptyState
                icon={<FileClock className="h-8 w-8" />}
                title="No audit logs"
                description="Administrative activity will appear here."
                className="py-10"
              />
            )}

          {/* Audit Success */}
          {!auditLogsIsLoading &&
            !auditLogsError &&
            auditLogs &&
            auditLogs.items.length > 0 && (
              <>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Date</TableHead>
                      <TableHead>Admin</TableHead>
                      <TableHead>Action</TableHead>
                      <TableHead>Entity</TableHead>
                      <TableHead>Detail</TableHead>
                    </TableRow>
                  </TableHeader>

                  <TableBody>
                    {auditLogs.items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell>{formatAuditDate(item.createdAt)}</TableCell>

                        <TableCell>{item.adminUsername}</TableCell>

                        <TableCell>{item.action}</TableCell>

                        <TableCell>
                          {item.entityType} #{item.entityId}
                        </TableCell>

                        <TableCell className="max-w-sm whitespace-normal">
                          {item.detail ?? "—"}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>

                <div className="mt-6">
                  <Pagination totalPages={auditLogs.totalPages} />
                </div>
              </>
            )}
        </CardContent>
      </Card>
    </div>
  );
}

export default AdminDashboardPage;
