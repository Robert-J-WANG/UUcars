import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { favoritesApi } from "@/api";
import { Button } from "@/components/ui/button";
import EmptyState from "@/components/EmptyState";
import { Heart, ExternalLink } from "lucide-react";

const PAGE_SIZE = 10;

export default function MyFavoritesPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  const { data, isLoading } = useQuery({
    queryKey: ["favorites", { page, pageSize: PAGE_SIZE }],
    queryFn: () => favoritesApi.getMyFavorites(page, PAGE_SIZE),
  });

  const removeMutation = useMutation({
    mutationFn: (carId: number) => favoritesApi.remove(carId),
    onSuccess: () => {
      toast.success("Removed from saved.");
      queryClient.invalidateQueries({ queryKey: ["favorites"] });
    },
    onError: (error) => toast.error(error.message),
  });

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <div
            key={i}
            className="h-28 animate-pulse rounded-[var(--radius-lg)]"
            style={{ backgroundColor: "var(--color-border)" }}
          />
        ))}
      </div>
    );
  }

  if (!data?.items.length) {
    return (
      <EmptyState
        icon={<Heart className="h-8 w-8" />}
        title="No saved cars"
        description="Save cars you're interested in and they'll appear here."
        actionLabel="Browse cars"
        onAction={() => navigate("/")}
      />
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
        {data.totalCount} saved car{data.totalCount !== 1 ? "s" : ""}
      </p>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {data.items.map((item) => (
          <div
            key={item.carId}
            className="flex flex-col justify-between rounded-[var(--radius-lg)] border p-4 space-y-3"
            style={{
              backgroundColor: "var(--color-surface)",
              borderColor: "var(--color-border)",
              boxShadow: "var(--shadow-card)",
            }}
          >
            <Link to={`/cars/${item.carId}`} className="space-y-1 group">
              <p
                className="font-medium text-sm leading-snug transition-colors group-hover:text-[var(--color-primary)]"
                style={{ color: "var(--color-text-primary)" }}
              >
                {item.car?.title ?? `Car #${item.carId}`}
              </p>
              {item.car && (
                <p
                  className="text-lg font-bold"
                  style={{
                    color: "var(--color-accent)",
                  }}
                >
                  ${item.car.price.toLocaleString()}
                </p>
              )}
              {item.car && (
                <p
                  className="text-xs"
                  style={{ color: "var(--color-text-muted)" }}
                >
                  {item.car.year} · {item.car.mileage?.toLocaleString()} km
                </p>
              )}
            </Link>
            <div
              className="flex gap-2 border-t pt-3"
              style={{ borderColor: "var(--color-border)" }}
            >
              <Button
                asChild
                variant="outline"
                size="sm"
                className="flex-1 gap-1"
              >
                <Link to={`/cars/${item.carId}`}>
                  <ExternalLink className="h-3 w-3" /> View
                </Link>
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="text-[var(--color-danger)] hover:bg-[var(--color-danger-light)] hover:text-[var(--color-danger)]"
                onClick={() => removeMutation.mutate(item.carId)}
                disabled={removeMutation.isPending}
              >
                Remove
              </Button>
            </div>
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
