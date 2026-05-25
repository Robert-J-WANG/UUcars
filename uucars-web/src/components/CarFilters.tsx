// src/components/CarFilters.tsx
import { useSearchParams } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { X } from "lucide-react";

function CarFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const brand = searchParams.get("brand") ?? "";
  const minPrice = searchParams.get("minPrice") ?? "";
  const maxPrice = searchParams.get("maxPrice") ?? "";

  const updateFilter = (key: string, value: string) => {
    const current = Object.fromEntries(searchParams.entries());
    if (value) {
      current[key] = value;
    } else {
      delete current[key];
    }
    delete current["page"];
    setSearchParams(current);
  };

  const clearFilters = () => setSearchParams({});

  const hasFilters = brand || minPrice || maxPrice;

  return (
    // 横向布局：所有过滤项一行排列，小屏时自动换行
    <div
      className="flex flex-wrap items-end gap-4 rounded-[var(--radius-lg)] border p-4 pb-8 mb-8"
      style={{
        borderColor: "var(--color-border)",
        backgroundColor: "var(--color-surface)",
      }}
    >
      {/* 品牌搜索 */}
      <div className="flex flex-col gap-1 min-w-[160px] flex-1">
        <Label
          htmlFor="brand"
          className="text-xs"
          style={{ color: "var(--color-text-secondary)" }}
        >
          Brand
        </Label>
        <Input
          id="brand"
          placeholder="e.g. BMW, Toyota"
          value={brand}
          onChange={(e) => updateFilter("brand", e.target.value)}
        />
      </div>

      {/* 最低价格 */}
      <div className="flex flex-col gap-1 min-w-[100px] flex-1">
        <Label
          htmlFor="minPrice"
          className="text-xs"
          style={{ color: "var(--color-text-secondary)" }}
        >
          Min Price
        </Label>
        <Input
          id="minPrice"
          placeholder="Min"
          type="number"
          value={minPrice}
          onChange={(e) => updateFilter("minPrice", e.target.value)}
        />
      </div>

      {/* 最高价格 */}
      <div className="flex flex-col gap-1 min-w-[100px] flex-1">
        <Label
          htmlFor="maxPrice"
          className="text-xs"
          style={{ color: "var(--color-text-secondary)" }}
        >
          Max Price
        </Label>
        <Input
          id="maxPrice"
          placeholder="Max"
          type="number"
          value={maxPrice}
          onChange={(e) => updateFilter("maxPrice", e.target.value)}
        />
      </div>

      {/* 清除按钮：只在有过滤条件时显示，与输入框底部对齐 */}
      {hasFilters && (
        <Button
          variant="outline"
          onClick={clearFilters}
          className="flex items-center gap-1.5 self-end"
          style={{ color: "var(--color-text-secondary)" }}
        >
          <X className="h-3.5 w-3.5" />
          Clear
        </Button>
      )}
    </div>
  );
}

export default CarFilters;
