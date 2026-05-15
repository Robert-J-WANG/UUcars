import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { adminApi } from "@/api";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";

export default function AdminPendingPage() {
  const queryClient = useQueryClient();

  /* -------------- 请求数据 -------------- */
  const { data, isLoading } = useQuery({
    queryKey: ["admin-pending"],
    queryFn: () => adminApi.getPendingCars(),
  });

  /* --------- approveMutation -------- */
  const approveMutation = useMutation({
    mutationFn: (carId: number) => adminApi.approve(carId),
    onSuccess: () => {
      toast.success("Car approved and published!");
      queryClient.invalidateQueries({ queryKey: ["admin-pending"] });
      // 公开列表也需要刷新，新车上架了
      queryClient.invalidateQueries({ queryKey: ["cars"] });
    },
    onError: (error) => toast.error(error.message),
  });

  /* --------- rejectMutation --------- */
  const rejectMutation = useMutation({
    mutationFn: (carId: number) => adminApi.reject(carId),
    onSuccess: () => {
      toast.success("Car rejected, returned to seller.");
      queryClient.invalidateQueries({ queryKey: ["admin-pending"] });
    },
    onError: (error) => toast.error(error.message),
  });

  /* --------- deleteMutation --------- */
  const deleteMutation = useMutation({
    mutationFn: (carId: number) => adminApi.deleteCar(carId),
    onSuccess: () => {
      toast.success("Car deleted.");
      queryClient.invalidateQueries({ queryKey: ["admin-pending"] });
    },
    onError: (error) => toast.error(error.message),
  });

  /* -------------- 数据加载 -------------- */
  if (isLoading) return <div>Loading...</div>;

  /* --------------- 空状态 -------------- */
  if (!data?.items.length) {
    return (
      <div className="py-12 text-center text-gray-500">
        No cars pending review. All caught up!
      </div>
    );
  }
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Title</TableHead>
          <TableHead>Seller</TableHead>
          <TableHead>Price</TableHead>
          <TableHead>Year</TableHead>
          <TableHead className="text-right">Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {data.items.map((car) => (
          <TableRow key={car.id}>
            <TableCell className="font-medium">{car.title}</TableCell>
            <TableCell>{car.sellerUsername}</TableCell>
            <TableCell>${car.price.toLocaleString()}</TableCell>
            <TableCell>{car.year}</TableCell>
            <TableCell>
              <div className="flex justify-end gap-2">
                <Button
                  size="sm"
                  onClick={() => approveMutation.mutate(car.id)}
                  disabled={
                    approveMutation.isPending ||
                    rejectMutation.isPending ||
                    deleteMutation.isPending
                  }
                >
                  Approve
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => rejectMutation.mutate(car.id)}
                  disabled={
                    approveMutation.isPending ||
                    rejectMutation.isPending ||
                    deleteMutation.isPending
                  }
                >
                  Reject
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={() => deleteMutation.mutate(car.id)}
                  disabled={
                    approveMutation.isPending ||
                    rejectMutation.isPending ||
                    deleteMutation.isPending
                  }
                >
                  Delete
                </Button>
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
