import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { ordersApi } from "@/api";
import { Button } from "@/components/ui/button";
import EmptyState from "@/components/EmptyState";
import { TrendingUp } from "lucide-react";

const PAGE_SIZE = 10;

const ORDER_STATUS: Record<
  string,
  { color: string; bg: string; label: string }
> = {
  Pending: {
    color: "var(--color-warning)",
    bg: "var(--color-warning-light)",
    label: "Pending",
  },
  Completed: {
    color: "var(--color-success)",
    bg: "var(--color-success-light)",
    label: "Completed",
  },
  Cancelled: {
    color: "var(--color-text-muted)",
    bg: "var(--color-border)",
    label: "Cancelled",
  },
};

function OrderStatusBadge({ status }: { status: string }) {
  const cfg = ORDER_STATUS[status] ?? ORDER_STATUS.Pending;
  return (
    <span
      className="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium"
      style={{ color: cfg.color, backgroundColor: cfg.bg }}
    >
      {cfg.label}
    </span>
  );
}

export default function MySalesPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  const { data, isLoading } = useQuery({
    queryKey: ["my-sales", { page, pageSize: PAGE_SIZE }],
    queryFn: () => ordersApi.getMySales(page, PAGE_SIZE),
  });

  const completeMutation = useMutation({
    mutationFn: (orderId: number) => ordersApi.complete(orderId),
    onSuccess: () => {
      toast.success("Order marked as completed.");
      queryClient.invalidateQueries({ queryKey: ["my-sales"] });
    },
    onError: (error) => toast.error(error.message),
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 4 }).map((_, i) => (
          <div
            key={i}
            className="h-24 animate-pulse rounded-[var(--radius-lg)]"
            style={{ backgroundColor: "var(--color-border)" }}
          />
        ))}
      </div>
    );
  }

  if (!data?.items.length) {
    return (
      <EmptyState
        icon={<TrendingUp className="h-8 w-8" />}
        title="No sales yet"
        description="When buyers purchase your cars, the orders will appear here."
        actionLabel="My listings"
        onAction={() => navigate("/profile/listings")}
      />
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
        {data.totalCount} sale{data.totalCount !== 1 ? "s" : ""}
      </p>

      <div className="space-y-3">
        {data.items.map((order) => (
          <div
            key={order.id}
            className="flex items-center justify-between rounded-[var(--radius-lg)] border p-4"
            style={{
              backgroundColor: "var(--color-surface)",
              borderColor: "var(--color-border)",
              boxShadow: "var(--shadow-card)",
            }}
          >
            <div className="space-y-1 flex-1 min-w-0 pr-4">
              <div className="flex items-center gap-2">
                <p
                  className="font-medium text-sm truncate"
                  style={{ color: "var(--color-text-primary)" }}
                >
                  {order.carTitle}
                </p>
                <OrderStatusBadge status={order.status} />
              </div>
              <p
                className="text-lg font-bold"
                style={{
                  color: "var(--color-accent)",
                }}
              >
                ${order.price.toLocaleString()}
              </p>
              <p
                className="text-xs"
                style={{ color: "var(--color-text-muted)" }}
              >
                Buyer: {order.buyerUsername}
              </p>
            </div>

            {order.status === "Pending" && (
              <Button
                size="sm"
                onClick={() => completeMutation.mutate(order.id)}
                disabled={completeMutation.isPending}
                className="shrink-0"
              >
                Mark Complete
              </Button>
            )}
          </div>
        ))}
      </div>

      {data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSearchParams({ page: String(page - 1) })}
            disabled={page === 1}
          >
            ← Prev
          </Button>
          <span
            className="text-sm"
            style={{ color: "var(--color-text-secondary)" }}
          >
            {page} / {data.totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setSearchParams({ page: String(page + 1) })}
            disabled={page === data.totalPages}
          >
            Next →
          </Button>
        </div>
      )}
    </div>
  );
}
