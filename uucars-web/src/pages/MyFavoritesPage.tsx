import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { favoritesApi } from "@/api";
import { Button } from "@/components/ui/button";

const PAGE_SIZE = 10;

export default function MyFavoritesPage() {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  /* -------------- 请求数据 -------------- */
  const { data, isLoading } = useQuery({
    queryKey: ["favorites", { page, pageSize: PAGE_SIZE }],
    queryFn: () => favoritesApi.getMyFavorites(page, PAGE_SIZE),
  });

  /* -------------- Mutations -------------- */
  const removeMutation = useMutation({
    mutationFn: (carId: number) => favoritesApi.remove(carId),
    onSuccess: () => {
      toast.success("Removed from favorites.");
      queryClient.invalidateQueries({ queryKey: ["favorites"] });
    },
    onError: (error) => toast.error(error.message),
  });

  const handlePageChange = (newPage: number) => {
    setSearchParams({ page: String(newPage) });
  };

  /* -------------- Loading -------------- */
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: PAGE_SIZE }).map((_, i) => (
          <div key={i} className="h-36 animate-pulse rounded-lg bg-gray-100" />
        ))}
      </div>
    );
  }

  /* -------------- 空状态 -------------- */
  if (!data?.items.length) {
    return (
      <div className="py-12 text-center text-gray-500">
        No favorites yet.{" "}
        <Link to="/" className="text-blue-600 hover:underline">
          Browse cars
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500 text-right">
        {data.totalCount} favorites
      </p>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {data.items.map((item) => (
          <div
            key={item.carId}
            className="flex flex-col justify-between rounded-lg border bg-white p-4 space-y-3"
          >
            <Link
              to={`/cars/${item.carId}`}
              className="space-y-1 hover:opacity-80"
            >
              <p className="font-medium">
                {item.car?.title ?? "Car #" + item.carId}
              </p>
              {item.car && (
                <p className="text-sm text-gray-500">
                  ${item.car.price.toLocaleString()} · {item.car.year}
                </p>
              )}
            </Link>
            <Button
              variant="outline"
              size="sm"
              className="w-full"
              onClick={() => removeMutation.mutate(item.carId)}
              disabled={removeMutation.isPending}
            >
              Remove
            </Button>
          </div>
        ))}
      </div>

      {/* 分页 */}
      {data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-4">
          <Button
            variant="outline"
            onClick={() => handlePageChange(page - 1)}
            disabled={page === 1}
          >
            Previous
          </Button>
          <span className="text-sm text-gray-600">
            Page {page} of {data.totalPages}
          </span>
          <Button
            variant="outline"
            onClick={() => handlePageChange(page + 1)}
            disabled={page === data.totalPages}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
