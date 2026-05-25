import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { adminApi } from "@/api";
import { Button } from "@/components/ui/button";
import EmptyState from "@/components/EmptyState";
import { CheckCircle, ExternalLink } from "lucide-react";
import { Link } from "react-router-dom";

export default function AdminPendingPage() {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ["admin-pending"],
    queryFn: () => adminApi.getPendingCars(),
  });

  const approveMutation = useMutation({
    mutationFn: (carId: number) => adminApi.approve(carId),
    onSuccess: () => {
      toast.success("Car approved and published!");
      queryClient.invalidateQueries({ queryKey: ["admin-pending"] });
      queryClient.invalidateQueries({ queryKey: ["cars"] });
    },
    onError: (error) => toast.error(error.message),
  });

  const rejectMutation = useMutation({
    mutationFn: (carId: number) => adminApi.reject(carId),
    onSuccess: () => {
      toast.success("Car returned to seller for revision.");
      queryClient.invalidateQueries({ queryKey: ["admin-pending"] });
    },
    onError: (error) => toast.error(error.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (carId: number) => adminApi.deleteCar(carId),
    onSuccess: () => {
      toast.success("Car removed.");
      queryClient.invalidateQueries({ queryKey: ["admin-pending"] });
    },
    onError: (error) => toast.error(error.message),
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <div
            key={i}
            className="h-20 animate-pulse rounded-[var(--radius-lg)]"
            style={{ backgroundColor: "var(--color-border)" }}
          />
        ))}
      </div>
    );
  }

  if (!data?.items.length) {
    return (
      <EmptyState
        icon={<CheckCircle className="h-8 w-8" />}
        title="All caught up!"
        description="No cars are waiting for review right now."
      />
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
        {data.totalCount} car{data.totalCount !== 1 ? "s" : ""} awaiting review
      </p>

      <div className="space-y-3">
        {data.items.map((car) => (
          <div
            key={car.id}
            className="flex flex-col gap-3 rounded-[var(--radius-lg)] border p-4 sm:flex-row sm:items-center sm:justify-between"
            style={{
              backgroundColor: "var(--color-surface)",
              borderColor: "var(--color-border)",
              boxShadow: "var(--shadow-card)",
            }}
          >
            <div className="space-y-1 flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <Link
                  to={`/cars/${car.id}`}
                  className="font-medium text-sm hover:text-[var(--color-primary)] transition-colors flex items-center gap-1"
                  style={{ color: "var(--color-text-primary)" }}
                >
                  {car.title}
                  <ExternalLink className="h-3 w-3 opacity-50" />
                </Link>
              </div>
              <p
                className="text-xs"
                style={{ color: "var(--color-text-muted)" }}
              >
                {car.brand} {car.model} · {car.year} ·{" "}
                {car.mileage.toLocaleString()} km ·{" "}
                <span style={{ color: "var(--color-accent)", fontWeight: 600 }}>
                  ${car.price.toLocaleString()}
                </span>{" "}
                · Seller:{" "}
                <span style={{ color: "var(--color-text-secondary)" }}>
                  {car.sellerUsername}
                </span>
              </p>
            </div>

            <div className="flex gap-2 shrink-0">
              <Button
                size="sm"
                onClick={() => approveMutation.mutate(car.id)}
                disabled={approveMutation.isPending}
                className="gap-1.5"
              >
                <CheckCircle className="h-3.5 w-3.5" />
                Approve
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={() => rejectMutation.mutate(car.id)}
                disabled={rejectMutation.isPending}
              >
                Reject
              </Button>
              <Button
                size="sm"
                variant="ghost"
                className="text-[var(--color-danger)] hover:bg-[var(--color-danger-light)] hover:text-[var(--color-danger)]"
                onClick={() => deleteMutation.mutate(car.id)}
                disabled={deleteMutation.isPending}
              >
                Remove
              </Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
