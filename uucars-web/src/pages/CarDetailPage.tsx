// src/pages/CarDetailPage.tsx
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { carsApi, favoritesApi, ordersApi } from "@/api";
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
import ImageGallery from "@/components/ImageGallery";

export default function CarDetailPage() {
  const { id } = useParams();
  const carId = Number(id);
  const { user, isAuthenticated } = useAuthStore();
  const queryClient = useQueryClient();
  const [dialogOpen, setDialogOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  /* ── 请求车辆详情 ── */
  const {
    data: car,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["car", carId],
    queryFn: () => carsApi.getById(carId),
    enabled: !isNaN(carId),
  });

  /* ── 收藏 mutation ── */
  const favoriteMutation = useMutation({
    mutationFn: () => favoritesApi.add(carId),
    onSuccess: () => {
      toast.success("Added to favorites!");
      queryClient.invalidateQueries({ queryKey: ["favorites"] });
    },
    onError: (error) => toast.error(error.message),
  });

  /* ── 下单 mutation ── */
  const orderMutation = useMutation({
    mutationFn: () => ordersApi.create({ carId }),
    onSuccess: () => {
      setDialogOpen(false);
      toast.success("Order placed successfully!");
      queryClient.invalidateQueries({ queryKey: ["car", carId] });
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

  /* ── 权限判断 ── */
  const isAdmin = user?.role === "Admin";
  const isOwner = user?.id === car?.sellerId;
  const canBuy =
    isAuthenticated() && !isAdmin && !isOwner && car?.status === "Published";

  const specs = [
    { label: "Year", value: car.year },
    { label: "Brand", value: car.brand },
    { label: "Model", value: car.model },
    { label: "Mileage", value: `${car.mileage.toLocaleString()} km` },
  ];

  return (
    <div className="mx-auto max-w-6xl">
      <div className="flex flex-col gap-8 lg:flex-row lg:items-start">
        {/* ══════════════════════════════════════
            左栏：图片画廊（55%）
            只负责展示图片，所有交互逻辑在 ImageGallery 内部
        ══════════════════════════════════════ */}
        <div className="flex-none lg:w-[55%]">
          <ImageGallery
            images={car.images ?? []}
            title={car.title}
            brand={car.brand}
          />
        </div>

        {/* ══════════════════════════════════════
            右栏：车辆信息 + 操作按钮（sticky）
            包含：标题/价格/状态、规格、描述、卖家、操作
        ══════════════════════════════════════ */}
        <div className="flex-1 lg:sticky lg:top-24">
          <div
            className="rounded-[var(--radius-xl)] border p-6 space-y-5"
            style={{
              backgroundColor: "var(--color-surface)",
              borderColor: "var(--color-border)",
              boxShadow: "var(--shadow-md)",
            }}
          >
            {/* ── 标题 + 状态 + 价格 ── */}
            <div>
              <div className="mb-1 flex items-start justify-between gap-3">
                <h1
                  className="text-lg "
                  style={{
                    color: "var(--color-text-primary)",
                  }}
                >
                  {car.title}
                </h1>
                <span
                  className="shrink-0 inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                  style={{
                    backgroundColor:
                      car.status === "Published"
                        ? "var(--color-success-light)"
                        : "var(--color-warning-light)",
                    color:
                      car.status === "Published"
                        ? "var(--color-success)"
                        : "var(--color-warning)",
                  }}
                >
                  {car.status}
                </span>
              </div>
              <p
                className="text-xl font-bold"
                style={{
                  color: "var(--color-accent)",
                }}
              >
                ${car.price.toLocaleString()}
              </p>
            </div>

            {/* ── 规格（原来在左栏底部，移到右栏） ── */}
            <div
              className="grid grid-cols-2 gap-3 rounded-[var(--radius-lg)] p-4"
              style={{
                backgroundColor: "var(--color-bg)",
                border: "1px solid var(--color-border)",
              }}
            >
              {specs.map((item) => (
                <div key={item.label}>
                  <p
                    className="text-xs uppercase tracking-wide"
                    style={{ color: "var(--color-text-muted)" }}
                  >
                    {item.label}
                  </p>
                  <p
                    className="mt-0.5 text-sm font-semibold"
                    style={{ color: "var(--color-text-primary)" }}
                  >
                    {item.value}
                  </p>
                </div>
              ))}
            </div>

            {/* ── 描述（原来在左栏底部，移到右栏） ── */}
            {car.description && (
              <div
                className="rounded-[var(--radius-lg)] p-4"
                style={{
                  backgroundColor: "var(--color-bg)",
                  border: "1px solid var(--color-border)",
                }}
              >
                <p
                  className="mb-1.5 text-xs font-semibold uppercase tracking-wide"
                  style={{ color: "var(--color-text-muted)" }}
                >
                  Description
                </p>
                <p
                  className="whitespace-pre-line text-sm leading-relaxed"
                  style={{ color: "var(--color-text-secondary)" }}
                >
                  {car.description}
                </p>
              </div>
            )}

            {/* ── 卖家信息 ── */}
            <div
              className="rounded-[var(--radius-md)] p-3"
              style={{
                backgroundColor: "var(--color-bg)",
                border: "1px solid var(--color-border)",
              }}
            >
              <p
                className="mb-0.5 text-xs uppercase tracking-wide"
                style={{ color: "var(--color-text-muted)" }}
              >
                Listed by
              </p>
              <p
                className="font-semibold text-sm"
                style={{ color: "var(--color-text-primary)" }}
              >
                {car.sellerUsername}
              </p>
            </div>

            {/* ── 操作按钮 ── */}
            <div
              className="space-y-2 border-t pt-4"
              style={{
                borderColor: "var(--color-border)",
                color: "var(--color-text-inverse)",
              }}
            >
              {!isAuthenticated() && (
                <Button asChild className="w-full" variant="default" size="lg">
                  <Link
                    to="/login"
                    state={{ from: { pathname: location.pathname } }}
                  >
                    Sign in to purchase
                  </Link>
                </Button>
              )}

              {isOwner && (
                <>
                  <Button asChild className="w-full">
                    <Link to={`/cars/${car.id}/edit`}>Edit Listing</Link>
                  </Button>
                  {car.status === "Draft" && (
                    <Button variant="outline" className="w-full">
                      Submit for Review
                    </Button>
                  )}
                </>
              )}

              {canBuy && (
                <div className="flex flex-col gap-6">
                  <Button
                    variant="outline"
                    className="w-full gap-2"
                    onClick={() => favoriteMutation.mutate()}
                    disabled={favoriteMutation.isPending}
                  >
                    {favoriteMutation.isPending ? "♡ Saving..." : "♡ Save Car"}
                  </Button>
                  <Button
                    className="w-full"
                    onClick={() => setDialogOpen(true)}
                  >
                    Buy Now
                  </Button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* ── 下单确认弹窗 ── */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent
          style={{
            background: "var(--color-surface)",
          }}
        >
          <DialogHeader>
            <DialogTitle>Confirm Purchase</DialogTitle>
            <DialogDescription>
              You are about to purchase{" "}
              <span className="font-semibold">{car.title}</span> for{" "}
              <span
                className="font-semibold"
                style={{ color: "var(--color-accent)" }}
              >
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
