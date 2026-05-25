// src/components/EmptyState.tsx
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface EmptyStateProps {
  /** 图标（lucide-react 的 SVG 组件） */
  icon?: React.ReactNode;
  title: string;
  description?: string;
  /** 操作按钮文字 */
  actionLabel?: string;
  /** 操作按钮点击回调 */
  onAction?: () => void;
  className?: string;
}

export default function EmptyState({
  icon,
  title,
  description,
  actionLabel,
  onAction,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center rounded-[var(--radius-xl)]",
        "border-2 border-dashed px-6 py-16 text-center",
        className,
      )}
      style={{ borderColor: "var(--color-border-strong)" }}
    >
      {/* 图标容器 */}
      {icon && (
        <div
          className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
          style={{ backgroundColor: "var(--color-primary-light)" }}
        >
          <div style={{ color: "var(--color-primary)" }}>{icon}</div>
        </div>
      )}

      {/* 标题 */}
      <h3
        className="mb-2 text-base font-semibold"
        style={{ color: "var(--color-text-primary)" }}
      >
        {title}
      </h3>

      {/* 描述 */}
      {description && (
        <p
          className="mb-6 max-w-xs text-sm leading-relaxed"
          style={{ color: "var(--color-text-secondary)" }}
        >
          {description}
        </p>
      )}

      {/* 操作按钮 */}
      {actionLabel && onAction && (
        <Button onClick={onAction} size="sm">
          {actionLabel}
        </Button>
      )}
    </div>
  );
}
