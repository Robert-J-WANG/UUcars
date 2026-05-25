// src/components/ImageGallery.tsx
// 独立的图片画廊组件：主图 + 缩略图 + 左右切换
import { useState } from "react";
import { ChevronLeft, ChevronRight, Car as CarIcon } from "lucide-react";

interface CarImage {
  id: number;
  imageUrl: string;
}

interface ImageGalleryProps {
  images: CarImage[];
  title: string;
  brand: string;
}

export default function ImageGallery({ images, title }: ImageGalleryProps) {
  const [activeIndex, setActiveIndex] = useState(0);

  const prev = () =>
    setActiveIndex((i) => (i - 1 + images.length) % images.length);
  const next = () => setActiveIndex((i) => (i + 1) % images.length);

  const hasImages = images && images.length > 0;
  const hasMultiple = hasImages && images.length > 1;

  return (
    <div className="space-y-3">
      {/* ── 主图区域 ── */}
      <div
        className="relative overflow-hidden"
        style={{
          aspectRatio: "4 / 3",
          borderRadius: "var(--radius-xl)",
          border: "1px solid var(--color-border)",
          backgroundColor: "var(--color-bg)",
        }}
      >
        {hasImages ? (
          <>
            <img
              key={activeIndex} // key 变化时触发重新渲染，产生淡入效果
              src={images[activeIndex].imageUrl}
              alt={`${title} - photo ${activeIndex + 1}`}
              className="h-full w-full object-cover transition-opacity duration-300"
            />

            {/* 左右箭头（多图时才显示） */}
            {hasMultiple && (
              <>
                <button
                  onClick={prev}
                  aria-label="Previous photo"
                  className="absolute left-3 top-1/2 -translate-y-1/2 flex h-9 w-9 items-center justify-center rounded-full transition-all hover:scale-105"
                  style={{
                    backgroundColor: "var(--color-text-muted)",
                    boxShadow: "var(--shadow-md)",
                    color: "var(--color-text-inverse)",
                    fontSize: "20px",
                  }}
                >
                  <ChevronLeft className="h-5 w-5" />
                </button>
                <button
                  onClick={next}
                  aria-label="Next photo"
                  className="absolute right-3 top-1/2 -translate-y-1/2 flex h-9 w-9 items-center justify-center rounded-full transition-all hover:scale-105"
                  style={{
                    backgroundColor: "var(--color-text-muted)",
                    boxShadow: "var(--shadow-md)",
                    color: "var(--color-text-inverse)",
                  }}
                >
                  <ChevronRight className="h-5 w-5" />
                </button>

                {/* 右下角计数器 */}
                <div
                  className="absolute bottom-3 right-3 rounded-full px-2.5 py-1 text-xs font-medium"
                  style={{
                    backgroundColor: "var(--color-text-muted)",
                    color: "var(--color-text-inverse)",
                    backdropFilter: "blur(4px)",
                  }}
                >
                  {activeIndex + 1} / {images.length}
                </div>
              </>
            )}
          </>
        ) : (
          // 无图占位
          <div className="flex h-full flex-col items-center justify-center gap-3">
            <CarIcon
              className="h-16 w-16 opacity-20"
              style={{ color: "var(--color-text-muted)" }}
            />
            <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
              No photos available
            </p>
          </div>
        )}
      </div>

      {/* ── 缩略图行（多图时显示） ── */}
      {hasMultiple && (
        <div className="flex gap-2 overflow-x-auto pb-1">
          {images.map((img, i) => (
            <button
              key={img.id}
              onClick={() => setActiveIndex(i)}
              aria-label={`View photo ${i + 1}`}
              className="shrink-0 overflow-hidden transition-all duration-150"
              style={{
                width: 72,
                height: 54,
                borderRadius: "var(--radius-md)",
                // 选中高亮，未选中降低透明度
                border:
                  i === activeIndex
                    ? "2px solid var(--color-primary)"
                    : "2px solid var(--color-border)",
                opacity: i === activeIndex ? 1 : 0.55,
              }}
            >
              <img
                src={img.imageUrl}
                alt={`Thumbnail ${i + 1}`}
                className="h-full w-full object-cover"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
