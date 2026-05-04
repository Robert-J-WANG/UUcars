import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { carsApi } from "@/api";
import CarCard from "@/components/CarCard";
import CarCardSkeleton from "@/components/CarCardSkeleton";
import { Button } from "@/components/ui/button";

// 配置每页显示的数量
// 前端单独维护显示的数量
const PAGE_SIZE = 10;

export default function HomePage() {
  const [page, setPage] = useState(1);

  const { data, isLoading, error } = useQuery({
    queryKey: ["cars", { page, pageSize: PAGE_SIZE }],
    queryFn: () => carsApi.getPaged({ page, pageSize: PAGE_SIZE }),
  });

  if (error) {
    return (
      <div className="py-12 text-center text-gray-500">
        Failed to load cars. Please try again.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Browse Cars</h1>

      {/* 车辆卡片网格 */}
      <div
        className="grid grid-cols-1 gap-4
                      sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4"
      >
        {isLoading
          ? // 加载中：显示 12 个骨架屏
            Array.from({ length: PAGE_SIZE }).map((_, i) => (
              <CarCardSkeleton key={i} />
            ))
          : // 加载完成：显示真实数据
            data?.items.map((car) => <CarCard key={car.id} car={car} />)}
      </div>

      {/* 没有数据时的提示 */}
      {!isLoading && data?.items.length === 0 && (
        <div className="py-12 text-center text-gray-500">
          No cars available at the moment.
        </div>
      )}

      {/* 分页控制 */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-4">
          <Button
            variant="outline"
            onClick={() => setPage((p) => p - 1)}
            disabled={page === 1}
          >
            Previous
          </Button>

          <span className="text-sm text-gray-600">
            Page {page} of {data.totalPages}
          </span>

          <Button
            variant="outline"
            onClick={() => setPage((p) => p + 1)}
            disabled={page === data.totalPages}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );
}
