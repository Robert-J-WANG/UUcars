// src/pages/MyListingsPage.tsx — 完整替换

import { carsApi } from "@/api";
import { Button } from "@/components/ui/button";
import EmptyState from "@/components/EmptyState";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Car, PlusCircle } from "lucide-react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import CarCardSkeleton from "@/components/CarCardSkeleton";
import ListingCard from "@/components/ListingCard";

const PAGE_SIZE = 10;

export default function MyListingsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  const { data, isLoading } = useQuery({
    queryKey: ["my-listings", { page, pageSize: PAGE_SIZE }],
    queryFn: () => carsApi.getMyListings({ page, pageSize: PAGE_SIZE }),
  });

  const submitMutation = useMutation({
    mutationFn: (carId: number) => carsApi.submit(carId),
    onSuccess: () => {
      toast.success("Submitted for review!");
      queryClient.invalidateQueries({ queryKey: ["my-listings"] });
    },
    onError: (error) => toast.error(error.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (carId: number) => carsApi.delete(carId),
    onSuccess: () => {
      toast.success("Car deleted.");
      queryClient.invalidateQueries({ queryKey: ["my-listings"] });
    },
    onError: (error) => toast.error(error.message),
  });

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <CarCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  if (!data?.items.length) {
    return (
      <EmptyState
        icon={<Car className="h-8 w-8" />}
        title="No listings yet"
        description="List your first car and reach thousands of buyers across New Zealand."
        actionLabel="List a car"
        onAction={() => navigate("/cars/new")}
      />
    );
  }

  return (
    <div className="space-y-6">
      {/* 顶部统计 + 新建按钮 */}
      <div className="flex items-center justify-between">
        <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
          {data.totalCount} listing{data.totalCount !== 1 ? "s" : ""}
        </p>
        <Button
          size="sm"
          onClick={() => navigate("/cars/new")}
          className="gap-1.5"
        >
          <PlusCircle className="h-3.5 w-3.5" />
          New Listing
        </Button>
      </div>

      {/* ✅ 卡片网格：统一高度 */}
      <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 xl:grid-cols-3 justify-items-center max-w-3xl xl:max-w-none mx-auto w-full">
        {data.items.map((car) => (
          <ListingCard
            key={car.id}
            car={car}
            onSubmit={submitMutation.mutate}
            onDelete={deleteMutation.mutate}
            isSubmitting={submitMutation.isPending}
            isDeleting={deleteMutation.isPending}
          />
        ))}
      </div>

      {/* 分页 */}
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
