import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { toast } from "sonner";
import { ordersApi } from "@/api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

const PAGE_SIZE = 10;

/* -------------- Badge variant -------------- */
const getStatusVariant = (status: string) => {
  switch (status) {
    case "Completed":
      return "outline";
    case "Cancelled":
      return "destructive";
    default:
      return "default";
  }
};

export default function MyPurchasesPage() {
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const page = Number(searchParams.get("page") ?? "1");

  /* -------------- 请求数据 -------------- */

  const { data, isLoading } = useQuery({
    queryKey: ["my-purchases", { page, pageSize: PAGE_SIZE }],
    queryFn: () => ordersApi.getMyPurchases(page, PAGE_SIZE),
  });

  /* -------------- Mutations -------------- */
  const cancelMutation = useMutation({
    mutationFn: (orderId: number) => ordersApi.cancel(orderId),
    onSuccess: () => {
      toast.success("Order cancelled.");
      queryClient.invalidateQueries({ queryKey: ["my-purchases"] });
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
      <div className="py-12 text-center text-gray-500">No purchases yet.</div>
    );
  }

  return (
    <div className="space-y-6">
      <p className="text-sm text-gray-500 text-right">
        {data.totalCount} orders
      </p>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {data.items.map((order) => (
          <div
            key={order.id}
            className="flex flex-col justify-between rounded-lg border bg-white p-4 space-y-3"
          >
            <div className="space-y-1">
              <div className="flex items-start justify-between gap-2">
                <span className="font-medium">{order.carTitle}</span>
                <Badge
                  variant={getStatusVariant(order.status)}
                  className="shrink-0"
                >
                  {order.status}
                </Badge>
              </div>
              <p className="text-lg font-bold text-blue-600">
                ${order.price.toLocaleString()}
              </p>
              <p className="text-sm text-gray-500">
                Seller: {order.sellerUsername}
              </p>
            </div>

            {order.status === "Pending" && (
              <Button
                variant="destructive"
                size="sm"
                className="w-full"
                onClick={() => cancelMutation.mutate(order.id)}
                disabled={cancelMutation.isPending}
              >
                Cancel
              </Button>
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
