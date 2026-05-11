import { carsApi } from "@/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { toast } from "sonner";

const PAGE_SIZE = 10;

/* -------------- Badge variant -------------- */
const getStatusVariant = (status: string) => {
  switch (status) {
    case "Published":
      return "secondary";
    case "PendingReview":
      return "outline";
    case "Sold":
      return "destructive";
    case "Draft":
    default:
      return "default";
  }
};

export default function MyListingsPage() {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  /* -------------- 请求数据 -------------- */
  const { data, isLoading } = useQuery({
    queryKey: ["my-listings", { page, pageSize: PAGE_SIZE }],
    queryFn: () => carsApi.getMyListings({ page, pageSize: PAGE_SIZE }),
  });

  /* -------------- Mutations -------------- */
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

  const handlePageChange = (newPage: number) => {
    setSearchParams({ page: String(newPage) });
  };

  /* -------------- Loading -------------- */
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: PAGE_SIZE }).map((_, i) => (
          <div key={i} className="h-48 animate-pulse rounded-lg bg-gray-100" />
        ))}
      </div>
    );
  }

  /* -------------- 空状态 -------------- */
  if (!data?.items.length) {
    return (
      <div className="py-12 text-center text-gray-500">
        You haven't listed any cars yet.{" "}
        <Link to="/cars/new" className="text-blue-600 hover:underline">
          List your first car
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* 顶部：数量统计 */}
      <p className="text-sm text-gray-500 text-right">
        {data.totalCount} cars total
      </p>

      {/* 网格列表 */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {data.items.map((car) => (
          <div
            key={car.id}
            className="flex flex-col justify-between rounded-lg border bg-white p-4 space-y-3"
          >
            {/* 车辆信息 */}
            <div className="space-y-1">
              <div className="flex items-start justify-between gap-2">
                <Link
                  to={`/cars/${car.id}`}
                  className="font-medium hover:text-blue-600 hover:underline"
                >
                  {car.title}
                </Link>
                <Badge
                  variant={getStatusVariant(car.status)}
                  className="shrink-0"
                >
                  {car.status}
                </Badge>
              </div>
              <p className="text-lg font-bold text-blue-600">
                ${car.price.toLocaleString()}
              </p>
              <p className="text-sm text-gray-500">
                {car.year} · {car.mileage.toLocaleString()} km
              </p>
            </div>

            {/* 操作按钮：只有 Draft 状态才有 */}
            {car.status === "Draft" && (
              <div className="flex gap-2 border-t pt-3">
                <Button asChild variant="outline" size="sm" className="flex-1">
                  <Link to={`/cars/${car.id}/edit`}>Edit</Link>
                </Button>
                <Button
                  size="sm"
                  className="flex-1"
                  onClick={() => submitMutation.mutate(car.id)}
                  disabled={submitMutation.isPending}
                >
                  Submit
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={() => deleteMutation.mutate(car.id)}
                  disabled={deleteMutation.isPending}
                >
                  Delete
                </Button>
              </div>
            )}
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
