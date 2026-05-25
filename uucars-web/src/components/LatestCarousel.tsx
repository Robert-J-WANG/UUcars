import { useState, useEffect } from "react";
import useEmblaCarousel from "embla-carousel-react";
import Autoplay from "embla-carousel-autoplay";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { carsApi } from "@/api";
import CarCardHorizontal from "@/components/CarCardHorizontal"; // 横版卡片

const CAROUSEL_SIZE = 6;

export default function LatestCarousel() {
  const { data } = useQuery({
    queryKey: ["cars-latest-carousel"],
    queryFn: () => carsApi.getPaged({ page: 1, pageSize: CAROUSEL_SIZE }),
    staleTime: 5 * 60 * 1000,
  });

  const [emblaRef, emblaApi] = useEmblaCarousel(
    {
      loop: true,
      align: "start",
      slidesToScroll: 1, // 每次滚动 1 张
    },
    [Autoplay({ delay: 3500, stopOnInteraction: true })],
  );

  const [selectedIndex, setSelectedIndex] = useState(0);

  useEffect(() => {
    if (!emblaApi) return;
    emblaApi.on("select", () => {
      setSelectedIndex(emblaApi.selectedScrollSnap());
    });
  }, [emblaApi]);

  if (!data?.items.length) return null;

  return (
    <section className="space-y-4">
      {/* 标题行 */}
      <div className="flex items-center justify-between">
        <h1
          className="text-lg"
          style={{
            color: "var(--color-text-primary)",
          }}
        >
          Latest Listings
        </h1>

        {/* 左右箭头 */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => emblaApi?.scrollPrev()}
            aria-label="Previous"
            className="flex h-8 w-8 items-center justify-center rounded-full border
                       border-[var(--color-border-strong)] transition-all duration-150
                       hover:border-[var(--color-primary)] hover:bg-[var(--color-primary-light)]"
            style={{ color: "var(--color-text-secondary)" }}
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <button
            onClick={() => emblaApi?.scrollNext()}
            aria-label="Next"
            className="flex h-8 w-8 items-center justify-center rounded-full border
                       border-[var(--color-border-strong)] transition-all duration-150
                       hover:border-[var(--color-primary)] hover:bg-[var(--color-primary-light)]"
            style={{ color: "var(--color-text-secondary)" }}
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* 轮播容器 */}
      <div ref={emblaRef} className="overflow-hidden">
        <div className="flex -ml-4">
          {data.items.map((car) => (
            // w-full（手机1张） / sm:w-1/2（平板、桌面2张）
            <div key={car.id} className="flex-none pl-4 w-full sm:w-1/2">
              <CarCardHorizontal car={car} />
            </div>
          ))}
        </div>
      </div>

      {/* 点导航 */}
      <div className="flex justify-center gap-1.5">
        {data.items.map((_, i) => (
          <button
            key={i}
            onClick={() => emblaApi?.scrollTo(i)}
            aria-label={`Slide ${i + 1}`}
            className="h-1.5 rounded-full transition-all duration-200"
            style={{
              width: i === selectedIndex ? "20px" : "6px",
              backgroundColor:
                i === selectedIndex
                  ? "var(--color-primary)"
                  : "var(--color-border-strong)",
            }}
          />
        ))}
      </div>
    </section>
  );
}
