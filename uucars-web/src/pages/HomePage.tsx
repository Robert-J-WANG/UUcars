import { useQuery } from "@tanstack/react-query";
import { carsApi } from "@/api";
import CarCard from "@/components/CarCard";
import CarCardSkeleton from "@/components/CarCardSkeleton";
import { Button } from "@/components/ui/button";
import { useSearchParams } from "react-router-dom";
import CarFilters from "@/components/CarFilters";
import useDebounce from "@/hooks/useDebounce";

// 配置每页显示的数量
// 前端单独维护显示的数量
const PAGE_SIZE = 10;

export default function HomePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  //  const [page, setPage] = useState(1);
  // 从 URL 读取当前过滤条件
  const brand = searchParams.get("brand") ?? "";
  const minPrice = searchParams.get("minPrice") ?? "";
  const maxPrice = searchParams.get("maxPrice") ?? "";
  const page = Number(searchParams.get("page") ?? "1");

  // 品牌加防抖：用户停止输入 500ms 后才触发请求
  const debouncedBrand = useDebounce(brand, 500);
  const debouncedMinPrice = useDebounce(minPrice, 800);
  const debouncedMaxPrice = useDebounce(maxPrice, 800);

  // 用防抖后的值作为 queryKey 和请求参数
  const { data, isLoading, error } = useQuery({
    // queryKey: ["cars", { page, pageSize: PAGE_SIZE }],
    // queryFn: () => carsApi.getPaged({ page, pageSize: PAGE_SIZE }),

    // queryKey 用防抖后的 brand，不用原始的 brand
    // 这样用户打字过程中 queryKey 不变，不触发请求
    // 停止输入 500ms 后 debouncedBrand 变化，queryKey 变化，才触发请求
    queryKey: [
      "cars",
      {
        page,
        pageSize: PAGE_SIZE,
        brand: debouncedBrand,
        minPrice: debouncedMinPrice,
        maxPrice: debouncedMaxPrice,
      },
    ],
    queryFn: () =>
      carsApi.getPaged({
        page,
        pageSize: PAGE_SIZE,
        brand: debouncedBrand || undefined,
        minPrice: debouncedMinPrice ? Number(debouncedMinPrice) : undefined,
        maxPrice: debouncedMaxPrice ? Number(debouncedMaxPrice) : undefined,
      }),
  });

  // 分页通过 URL 参数控制
  const handlePageChange = (newPage: number) => {
    const current = Object.fromEntries(searchParams.entries());
    setSearchParams({ ...current, page: String(newPage) });
  };

  if (error) {
    return (
      <div className="py-12 text-center text-gray-500">
        Failed to load cars. Please try again.
      </div>
    );
  }

  return (
    <div className="flex gap-6">
      {/* 左侧：过滤条件 */}
      <aside className="hidden w-64 shrink-0 lg:block">
        <CarFilters />
      </aside>

      {/* 右侧：列表 */}
      <div className="flex-1 space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold">Browse Cars</h1>
          {data && (
            <p className="text-sm text-gray-500">
              {data.totalCount} cars found
            </p>
          )}
        </div>

        {/* 车辆卡片网格 */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {isLoading
            ? Array.from({ length: PAGE_SIZE }).map((_, i) => (
                <CarCardSkeleton key={i} />
              ))
            : data?.items.map((car) => <CarCard key={car.id} car={car} />)}
        </div>

        {/* 没有数据 */}
        {!isLoading && data?.items.length === 0 && (
          <div className="py-12 text-center text-gray-500">
            No cars found. Try adjusting your filters.
          </div>
        )}

        {/* 分页 */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-center gap-4">
            <Button
              variant="outline"
              onClick={() => handlePageChange(page - 1)}
              disabled={page === 1}
            >
              Previous
            </Button>
            <span className="text-sm text-gray-600">
              Page {page} of {data.totalPages}
            </span>
            <Button
              variant="outline"
              onClick={() => handlePageChange(page + 1)}
              disabled={page === data.totalPages}
            >
              Next
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
