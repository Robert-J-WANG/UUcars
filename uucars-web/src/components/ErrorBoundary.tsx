import {
  ErrorBoundary as ReactErrorBoundary,
  type FallbackProps,
} from "react-error-boundary";
import { Button } from "./ui/button";

function ErrorFallback({ error, resetErrorBoundary }: FallbackProps) {
  return (
    <div
      className="flex min-h-50 flex-col items-center justify-center gap-4 rounded-xl border p-8 text-center"
      style={{
        borderColor: "var(--color-border)",
        backgroundColor: "var(--color-surface)",
      }}
    >
      <p
        className="text-lg font-semibold"
        style={{ color: "var(--color-text-primary)" }}
      >
        Something went wrong
      </p>
      {/* 只在开发环境显示具体错误信息，方便调试。
          生产环境不暴露内部实现细节，避免给攻击者提供信息。 */}
      {import.meta.env.DEV && (
        <p
          className="max-w-md text-xs"
          style={{ color: "var(--color-danger)" }}
        >
          {error instanceof Error ? error.message : "Unknown error"}
        </p>
      )}
      <Button variant="outline" onClick={resetErrorBoundary}>
        Try again
      </Button>
    </div>
  );
}

export default function ErrorBoundary({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ReactErrorBoundary FallbackComponent={ErrorFallback}>
      {children}
    </ReactErrorBoundary>
  );
}
