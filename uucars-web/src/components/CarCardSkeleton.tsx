// src/components/CarCardSkeleton.tsx
export default function CarCardSkeleton() {
  return (
    // max-w-sm mx-auto：与 CarCard 的 Link 外层保持一致，卡片不无限拉伸
    <div
      className="w-full max-w-sm mx-auto rounded-[var(--radius-lg)] border overflow-hidden"
      style={{
        backgroundColor: "var(--color-surface)",
        borderColor: "var(--color-border)",
        boxShadow: "var(--shadow-card)",
      }}
      aria-hidden="true"
    >
      {/* 图片占位：aspect-[4/3] 与 CarCard 图片区完全对齐，宽度变化时等比缩放 */}
      <div
        className="aspect-[4/3] skeleton-shimmer"
        style={{ backgroundColor: "var(--color-border)" }}
      />

      {/* 内容占位 */}
      <div className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <div
            className="h-5 w-16 rounded-full skeleton-shimmer"
            style={{ backgroundColor: "var(--color-border)" }}
          />
          <div
            className="h-4 w-10 rounded skeleton-shimmer"
            style={{ backgroundColor: "var(--color-border)" }}
          />
        </div>
        <div
          className="h-4 w-full rounded skeleton-shimmer"
          style={{ backgroundColor: "var(--color-border)" }}
        />
        <div
          className="h-4 w-3/4 rounded skeleton-shimmer"
          style={{ backgroundColor: "var(--color-border)" }}
        />
        <div
          className="h-7 w-28 rounded skeleton-shimmer"
          style={{ backgroundColor: "var(--color-border)" }}
        />
        <div className="flex items-center justify-between">
          <div
            className="h-4 w-24 rounded skeleton-shimmer"
            style={{ backgroundColor: "var(--color-border)" }}
          />
          <div
            className="h-5 w-16 rounded-full skeleton-shimmer"
            style={{ backgroundColor: "var(--color-border)" }}
          />
        </div>
      </div>
    </div>
  );
}
