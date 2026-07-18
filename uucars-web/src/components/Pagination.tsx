import { useSearchParams } from "react-router-dom";
import { Button } from "./ui/button";

function Pagination({ totalPages }: { totalPages: number }) {
  const [searchParams, setSearchParams] = useSearchParams();

  const page = Number(searchParams.get("page") ?? "1");

  const handlePageChange = (newPage: number) => {
    const current = Object.fromEntries(searchParams.entries());
    setSearchParams({ ...current, page: String(newPage) });
  };
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-center gap-3">
      <Button
        variant="outline"
        size="sm"
        onClick={() => handlePageChange(page - 1)}
        disabled={page === 1}
      >
        ← Prev
      </Button>
      <span
        className="text-sm"
        style={{ color: "var(--color-text-secondary)" }}
      >
        {page} / {totalPages}
      </span>
      <Button
        variant="outline"
        size="sm"
        onClick={() => handlePageChange(page + 1)}
        disabled={page === totalPages}
      >
        Next →
      </Button>
    </div>
  );
}

export default Pagination;
