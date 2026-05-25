// src/components/CarCardHorizontal.tsx
import { Link } from "react-router-dom";
import { Gauge, Calendar, Car as CarIcon } from "lucide-react";
import type { Car } from "@/types";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

interface CarCardHorizontalProps {
  car: Car;
}

// 与 CarCard 保持一致的相对时间函数
function getRelativeTime(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const days = Math.floor(diff / 86400000);
  return `${days}d ago`;
}

export default function CarCardHorizontal({ car }: CarCardHorizontalProps) {
  return (
    <Link to={`/cars/${car.id}`} className="block group">
      <article
        className={cn(
          "flex rounded-[var(--radius-lg)] border overflow-hidden",
          "bg-[var(--color-surface)] shadow-[var(--shadow-card)] card-hover",
          "h-72",
        )}
        style={{ borderColor: "var(--color-border)" }}
      >
        {/* 左侧图片区域
            h-full 撑满卡片高度，aspect-[4/3] 由高度反推宽度
            与 CarCard 保持相同比例                            */}
        <div className="relative shrink-0 h-full aspect-[4/3] overflow-hidden">
          {car.coverImageUrl ? (
            // 有图片：显示真实图片
            <img
              src={car.coverImageUrl}
              alt={car.title}
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            />
          ) : (
            // 无图片：与 CarCard 一致的占位样式
            <div
              className="flex h-full w-full items-center justify-center"
              style={{ backgroundColor: "var(--color-border)" }}
            >
              <CarIcon
                className="h-10 w-10 opacity-20"
                style={{ color: "var(--color-text-muted)" }}
              />
            </div>
          )}
        </div>

        {/* 右侧文字区域 */}
        <div className="flex min-w-0 flex-1 flex-col justify-between p-3">
          {/* 上：品牌 + 年份 */}
          <div>
            <div className="mb-1.5 flex items-center justify-between">
              <Badge variant="accent" className="text-xs ">
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
            <h3
              className={cn(
                "line-clamp-2 text-sm font-semibold leading-snug",
                "transition-colors duration-150 group-hover:text-[var(--color-primary)]",
              )}
              style={{ color: "var(--color-text-primary)" }}
            >
              {car.title}
            </h3>
          </div>

          {/* 下：价格 + 里程 + 发布时间 */}
          <div>
            <span
              className="block text-lg font-bold"
              style={{
                color: "var(--color-accent)",
                fontFamily: "'DM Serif Display', serif",
              }}
            >
              ${car.price.toLocaleString()}
            </span>
            <div className="mt-1 flex items-center justify-between">
              <span
                className="flex items-center gap-1 text-xs"
                style={{ color: "var(--color-text-secondary)" }}
              >
                <Gauge className="h-3 w-3" />
                {car.mileage.toLocaleString()} km
              </span>
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
        </div>
      </article>
    </Link>
  );
}
