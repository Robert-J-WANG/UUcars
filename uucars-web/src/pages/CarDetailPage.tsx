import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { carsApi, favoritesApi, ordersApi } from "@/api";
import { Badge } from "@/components/ui/badge";
import { useAuthStore } from "@/stores/authStore";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export default function CarDetailPage() {
  const { id } = useParams();
  const carId = Number(id);
  // 在组件里获取当前用户
  const { user, isAuthenticated } = useAuthStore();
  // useQueryClient：获取 QueryClient 实例，用于操作缓存
  const queryClient = useQueryClient();
  // 控制下单对话框
  const [dialogOpen, setDialogOpen] = useState(false);
  const navigate = useNavigate();
  // 页面路由信息
  const location = useLocation();

  /* ------------- 请求车辆详情 ------------- */
  const {
    data: car,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["car", carId],
    queryFn: () => carsApi.getById(carId),
    enabled: !isNaN(carId),
  });

  /* ----------- 收藏 mutation ---------- */

  const favoriteMutation = useMutation({
    mutationFn: () => favoritesApi.add(carId),
    onSuccess: () => {
      toast.success("Added to favorites!");
      // 让收藏列表的缓存失效，下次访问收藏页时会重新请求
      queryClient.invalidateQueries({ queryKey: ["favorites"] });
    },
    onError: (error) => {
      toast.error(error.message);
    },
  });
  /* ----------- 下单 mutation ---------- */
  const orderMutation = useMutation({
    mutationFn: () => ordersApi.create({ carId }),
    onSuccess: () => {
      setDialogOpen(false);
      toast.success("Order placed successfully!");
      // 让车辆详情缓存失效（车辆状态变成 Sold 了）
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
      // 让列表缓存失效（这辆车不再出现在列表里）
      queryClient.invalidateQueries({ queryKey: ["cars"] });
      navigate("/profile/purchases");
    },
    onError: (error) => {
      setDialogOpen(false);
      toast.error(error.message);
    },
  });

  if (isLoading) return <div className="p-8">Loading...</div>;
  if (error || !car) return <div className="p-8">Car not found.</div>;

  /* -------------- 权限判断 -------------- */

  // 判断当前用户是不是admin
  const isAdmin = user?.role === "Admin";
  // 判断当前用户是不是这辆车的车主
  const isOwner = user?.id === car?.sellerId;
  // 判断是否可以购买：已登录 + 不是admin + 不是车主  + 车辆是 Published 状态
  const canBuy =
    isAuthenticated() && !isAdmin && !isOwner && car?.status === "Published";

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {/* 图片区域 */}
      {car.images.length > 0 ? (
        <div className="overflow-hidden rounded-lg">
          <img
            src={car.images[0].imageUrl}
            alt={car.title}
            className="h-96 w-full object-cover"
          />
        </div>
      ) : (
        <div
          className="flex h-96 items-center justify-center
                        rounded-lg bg-gray-100 text-gray-400"
        >
          No images available
        </div>
      )}

      {/* 基本信息 */}
      <div className="space-y-2">
        <div className="flex items-start justify-between">
          <h1 className="text-2xl font-bold">{car.title}</h1>
          <Badge variant="secondary">{car.status}</Badge>
        </div>
        <p className="text-3xl font-bold text-blue-600">
          ${car.price.toLocaleString()}
        </p>
        <p className="text-gray-500">
          {car.year} · {car.brand} {car.model} · {car.mileage.toLocaleString()}{" "}
          km
        </p>
        <p className="text-sm text-gray-500">Seller: {car.sellerUsername}</p>
      </div>

      {/* 描述 */}
      {car.description && (
        <div className="space-y-2">
          <h2 className="font-semibold">Description</h2>
          <p className="whitespace-pre-line text-gray-600">{car.description}</p>
        </div>
      )}

      {/* 操作按钮:权限判断，显示不同按钮   */}
      <div className="border-t pt-4">
        {!isAuthenticated() && (
          // 未登录：引导去登录
          <Button asChild className="w-full">
            <Link
              to="/login"
              // 带上当前页面路径作为 state
              state={{ from: { pathname: location.pathname } }}
            >
              Sign in to purchase
            </Link>
          </Button>
        )}

        {isOwner && (
          // 车主：显示管理操作
          <div className="flex gap-2">
            <Button asChild variant="outline" className="flex-1">
              <Link to={`/cars/${car.id}/edit`}>Edit</Link>
            </Button>
            {car.status === "Draft" && (
              <Button asChild className="flex-1">
                <Link to={`/cars/${car.id}/submit`}>Submit for Review</Link>
              </Button>
            )}
          </div>
        )}

        {canBuy && (
          <div className="flex gap-2">
            <Button
              variant="outline"
              // 调用mutate()执行更新
              onClick={() => favoriteMutation.mutate()}
              disabled={favoriteMutation.isPending}
            >
              {favoriteMutation.isPending ? "Saving..." : "♡ Favorite"}
            </Button>
            <Button className="flex-1" onClick={() => setDialogOpen(true)}>
              Buy Now
            </Button>
          </div>
        )}
      </div>

      {/* 下单确认弹窗 */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Purchase</DialogTitle>
            <DialogDescription>
              You are about to purchase{" "}
              <span className="font-semibold">{car.title}</span> for{" "}
              <span className="font-semibold text-blue-600">
                ${car.price.toLocaleString()}
              </span>
              . This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDialogOpen(false)}
              disabled={orderMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              onClick={() => orderMutation.mutate()}
              disabled={orderMutation.isPending}
            >
              {orderMutation.isPending ? "Processing..." : "Confirm Purchase"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
