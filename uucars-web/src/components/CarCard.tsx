// src/components/CarCard.tsx
import { Link } from "react-router-dom";
import { Gauge, Calendar, Car as CarIcon } from "lucide-react";
import type { Car } from "@/types";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

interface CarCardProps {
  car: Car;
}

// 计算距今多久（用于显示发布时间）
function getRelativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const days = Math.floor(diff / 86400000);
  return `${days}d ago`;
}

export default function CarCard({ car }: CarCardProps) {
  return (
    <Link
      to={`/cars/${car.id}`}
      className="block group w-full max-w-sm mx-auto"
    >
      <article
        className={cn(
          "rounded-[var(--radius-lg)] border overflow-hidden",
          "bg-[var(--color-surface)]",
          "shadow-[var(--shadow-card)] card-hover",
        )}
        style={{ borderColor: "var(--color-border)" }}
      >
        {/* 图片区域：aspect-[4/3] 固定比例，宽度变化时高度等比缩放 */}
        <div className="relative aspect-[4/3] overflow-hidden">
          {car.coverImageUrl ? (
            // 有图片：直接显示，object-cover 保证不变形
            <img
              src={car.coverImageUrl}
              alt={car.title}
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            />
          ) : (
            // 无图片：lucide Car 图标做占位
            <div
              className="flex h-full w-full items-center justify-center"
              style={{ backgroundColor: "var(--color-border)" }}
            >
              <CarIcon
                className="h-16 w-16 opacity-20"
                style={{ color: "var(--color-text-muted)" }}
              />
            </div>
          )}
        </div>

        {/* 信息区 */}
        <div className="p-4">
          {/* 品牌 + 年份 */}
          <div className="mb-2 flex items-center justify-between">
            <Badge variant="accent" className="text-xs">
              {car.brand}
            </Badge>
            <span
              className="flex items-center gap-1 text-xs"
              style={{ color: "var(--color-text-muted)" }}
            >
              <Calendar className="h-3 w-3" />
              {car.year}
            </span>
          </div>

          {/* 标题 */}
          <h3
            className={cn(
              "mb-3 line-clamp-2 text-sm font-semibold leading-snug",
              "transition-colors duration-150 group-hover:text-[var(--color-primary)]",
              "text-nowrap overflow-hidden text-ellipsis",
            )}
            style={{ color: "var(--color-text-primary)" }}
          >
            {car.title}
          </h3>

          {/* 价格 */}
          <div className="mb-3">
            <span
              className="text-xl font-bold text-gradient-accent"
              style={{
                // color: "var(--color-primary)",
                fontFamily: "'DM Serif Display', serif",
              }}
            >
              ${car.price.toLocaleString()}
            </span>
          </div>

          {/* 里程 + 发布时间 */}
          <div className="flex items-center justify-between">
            <span
              className="flex items-center gap-1 text-xs"
              style={{ color: "var(--color-text-secondary)" }}
            >
              <Gauge className="h-3 w-3" />
              {car.mileage.toLocaleString()} km
            </span>
            {/* 发布时间：替换原来的 MileageBadge */}
            {car.createdAt && (
              <span
                className="text-xs"
                style={{ color: "var(--color-text-muted)" }}
              >
                {getRelativeTime(car.createdAt)}
              </span>
            )}
          </div>
        </div>
      </article>
    </Link>
  );
}
