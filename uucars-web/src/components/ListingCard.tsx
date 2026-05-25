import type { Car } from "@/types";
import { Button } from "./ui/button";
import { Link } from "react-router-dom";

interface ListingCardProps {
  car: Car;
  onSubmit: (id: number) => void;
  onDelete: (id: number) => void;

  isSubmitting?: boolean;
  isDeleting?: boolean;
}

// ✅ 统一状态样式配置，彻底解决换行问题
const STATUS_CONFIG: Record<
  string,
  { label: string; color: string; bg: string }
> = {
  Published: {
    label: "Published",
    color: "var(--color-success)",
    bg: "var(--color-success-light)",
  },
  PendingReview: {
    label: "Pending",
    color: "var(--color-warning)",
    bg: "var(--color-warning-light)",
  },
  // ✅ 重点：PendingReview label 改为 'Pending'，避免两个英文单词换行
  Draft: {
    label: "Draft",
    color: "var(--color-text-secondary)",
    bg: "var(--color-bg)",
  },
  Sold: {
    label: "Sold",
    color: "var(--color-danger)",
    bg: "var(--color-danger-light)",
  },
  Deleted: {
    label: "Deleted",
    color: "var(--color-text-muted)",
    bg: "var(--color-border)",
  },
};

function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status] ?? STATUS_CONFIG.Draft;
  return (
    <span
      // ✅ whitespace-nowrap + shrink-0 双重保证不换行
      className="inline-flex shrink-0 items-center whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-medium"
      style={{ color: cfg.color, backgroundColor: cfg.bg }}
    >
      {cfg.label}
    </span>
  );
}

export default function ListingCard({
  car,
  onSubmit,
  onDelete,
  isSubmitting,
  isDeleting,
}: ListingCardProps) {
  return (
    <div
      // ✅ flex flex-col 让内容撑满，按钮区 mt-auto 沉底
      // min-h 保证无按钮的卡片和有按钮的卡片视觉高度接近
      className="block group w-full max-w-sm mx-auto p-4 border overflow-hidden
         card-hover"
      style={{
        backgroundColor: "var(--color-surface)",
        borderColor: "var(--color-border)",
        boxShadow: "var(--shadow-card)",
        minHeight: "176px",
        borderRadius: "var(--radius-lg)",
      }}
    >
      {/* 车辆信息区 */}
      <div className="flex-1 space-y-2">
        {/* 标题行：标题 + 状态，状态 shrink-0 不压缩 */}
        <div className="flex items-start justify-between gap-2">
          <Link
            to={`/cars/${car.id}`}
            className="line-clamp-2 flex-1 text-sm font-medium leading-snug transition-colors"
            style={{ color: "var(--color-text-primary)" }}
          >
            {car.title}
          </Link>
          {/* ✅ StatusBadge 内部已有 shrink-0 + whitespace-nowrap */}
          <StatusBadge status={car.status} />
        </div>

        <p
          className="text-lg font-bold"
          style={{
            color: "var(--color-accent)",
          }}
        >
          ${car.price.toLocaleString()}
        </p>
        <p className="text-xs" style={{ color: "var(--color-text-muted)" }}>
          {car.year} · {car.mileage.toLocaleString()} km
        </p>
      </div>

      {/* ✅ 操作区：mt-auto 沉到底部，始终占位保持高度统一 */}
      <div
        className="mt-4 border-t pt-3"
        style={{ borderColor: "var(--color-border)" }}
      >
        {car.status === "Draft" ? (
          // Draft：Edit + Submit + Delete
          <div className="flex gap-2">
            <Button asChild variant="outline" size="sm" className="flex-1">
              <Link to={`/cars/${car.id}/edit`}>Edit</Link>
            </Button>
            <Button
              size="sm"
              className="flex-1"
              onClick={() => onSubmit(car.id)}
              disabled={isSubmitting}
            >
              Submit
            </Button>
            <Button
              size="sm"
              variant="destructive"
              className="w-8 px-0"
              onClick={() => onDelete(car.id)}
              disabled={isDeleting}
              aria-label="Delete"
            >
              ✕
            </Button>
          </div>
        ) : (
          // 非 Draft：显示 View 按钮（禁用的操作按钮区域，保持高度一致）
          <Button
            asChild
            variant="ghost"
            size="sm"
            className="w-full text-xs"
            style={{ color: "var(--color-text-muted)" }}
          >
            <Link to={`/cars/${car.id}`}>View listing →</Link>
          </Button>
        )}
      </div>
    </div>
  );
}
