import { useSearchParams } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";

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

  const clearFilters = () => {
    setSearchParams({});
  };

  const hasFilters = brand || minPrice || maxPrice;

  return (
    <div className="rounded-lg border bg-white p-4 space-y-4">
      <h2 className="font-semibold">Filters</h2>

      {/* 品牌搜索 */}
      <div className="space-y-1">
        <Label htmlFor="brand">Brand</Label>
        <Input
          id="brand"
          placeholder="e.g. BMW, Toyota"
          value={brand}
          onChange={(e) => updateFilter("brand", e.target.value)}
        />
      </div>

      {/* 价格区间 */}
      <div className="space-y-1">
        <Label>Price Range</Label>
        <div className="flex gap-2">
          <Input
            placeholder="Min"
            type="number"
            value={minPrice}
            onChange={(e) => updateFilter("minPrice", e.target.value)}
          />
          <Input
            placeholder="Max"
            type="number"
            value={maxPrice}
            onChange={(e) => updateFilter("maxPrice", e.target.value)}
          />
        </div>
      </div>

      {/* 清空过滤条件 */}
      {hasFilters && (
        <Button variant="outline" className="w-full" onClick={clearFilters}>
          Clear filters
        </Button>
      )}
    </div>
  );
}

export default CarFilters;
