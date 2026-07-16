// src/pages/HomePage.tsx
import { useQuery } from "@tanstack/react-query";
import { carsApi } from "@/api";
import CarCard from "@/components/CarCard";
import CarCardSkeleton from "@/components/CarCardSkeleton";
import { useSearchParams } from "react-router-dom";
import CarFilters from "@/components/CarFilters";
import useDebounce from "@/hooks/useDebounce";
import { Car } from "lucide-react";
import EmptyState from "@/components/EmptyState";
import HeroBanner from "@/components/HeroBanner";
import LatestCarousel from "@/components/LatestCarousel";
import SellerBanner from "@/components/SellerBanner";
import Pagination from "@/components/Pagination";

const PAGE_SIZE = 6;

export default function HomePage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const brand = searchParams.get("brand") ?? "";
  const minPrice = searchParams.get("minPrice") ?? "";
  const maxPrice = searchParams.get("maxPrice") ?? "";
  //读取 年份URL 参数
  const minYear = searchParams.get("minYear") ?? "";
  const maxYear = searchParams.get("maxYear") ?? "";
  const page = Number(searchParams.get("page") ?? "1");

  const debouncedBrand = useDebounce(brand, 800);
  const debouncedMinPrice = useDebounce(minPrice, 800);
  const debouncedMaxPrice = useDebounce(maxPrice, 800);
  // 年份防抖
  const debouncedMinYear = useDebounce(minYear, 800);
  const debouncedMaxYear = useDebounce(maxYear, 800);

  const { data, isLoading, error } = useQuery({
    queryKey: [
      "cars",
      {
        page,
        pageSize: PAGE_SIZE,
        brand: debouncedBrand,
        minPrice: debouncedMinPrice,
        maxPrice: debouncedMaxPrice,
        minYear: debouncedMinYear, // ✅ 新增
        maxYear: debouncedMaxYear, // ✅ 新增
      },
    ],
    queryFn: () =>
      carsApi.getPaged({
        page,
        pageSize: PAGE_SIZE,
        brand: debouncedBrand || undefined,
        minPrice: debouncedMinPrice ? Number(debouncedMinPrice) : undefined,
        maxPrice: debouncedMaxPrice ? Number(debouncedMaxPrice) : undefined,
        minYear: debouncedMinYear ? Number(debouncedMinYear) : undefined, // ✅ 新增
        maxYear: debouncedMaxYear ? Number(debouncedMaxYear) : undefined, // ✅ 新增
      }),
  });

  const isHomepage =
    !brand && !minPrice && !maxPrice && !minYear && !maxYear && page === 1;

  if (error) {
    return (
      <div
        className="py-12 text-center"
        style={{ color: "var(--color-text-muted)" }}
      >
        Failed to load cars. Please try again.
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Hero + 轮播（首页状态显示） */}
      {isHomepage && <HeroBanner />}
      {isHomepage && <LatestCarousel />}
      {isHomepage && <SellerBanner />}

      {/* 车辆列表区域（单栏，filter 移至顶部） */}
      <div className="space-y-5">
        {/* 标题 + 统计 */}
        <div className="flex items-center justify-between">
          <h1
            className="text-lg"
            style={{
              color: "var(--color-text-primary)",
            }}
          >
            {isHomepage ? "All Listings" : "Browse Cars"}
          </h1>
          {data && (
            <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
              {data.totalCount.toLocaleString()} cars found
            </p>
          )}
        </div>

        {/* Filters：横向排列在列表上方 */}
        <CarFilters />

        {/* 车辆卡片网格
            justify-items-center：配合 CarCard 的 max-w-sm，让卡片在格子里居中
            而不是被拉伸到格子满宽                                              */}
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 xl:grid-cols-3 justify-items-center max-w-3xl xl:max-w-none mx-auto w-full">
          {isLoading
            ? Array.from({ length: PAGE_SIZE }).map((_, i) => (
                <CarCardSkeleton key={i} />
              ))
            : // 改之后
              data?.items.map((car) => (
                <CarCard
                  key={car.id}
                  car={car}
                  highlightKeyword={debouncedBrand || undefined}
                />
              ))}
        </div>

        {/* 空状态 */}
        {!isLoading && data?.items.length === 0 && (
          <EmptyState
            icon={<Car className="h-8 w-8" />}
            title="No cars found"
            description="Try adjusting your filters."
            actionLabel="Clear filters"
            onAction={() => setSearchParams({})}
          />
        )}

        {/* 分页 */}
        <Pagination totalPages={data?.totalPages ?? 0} />
      </div>
    </div>
  );
}
