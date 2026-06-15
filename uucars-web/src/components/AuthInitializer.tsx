import { useEffect } from "react";
import { useAuthStore } from "@/stores/authStore";

export default function AuthInitializer({
  children,
}: {
  children: React.ReactNode;
}) {
  const initialize = useAuthStore((state) => state.initialize);
  const isInitializing = useAuthStore((state) => state.isInitializing);

  useEffect(() => {
    initialize();
  }, [initialize]);

  if (isInitializing) {
    return (
      <div
        className="flex h-screen w-full items-center justify-center"
        style={{ backgroundColor: "var(--color-bg)" }}
      >
        <div className="flex items-center gap-2">
          <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="var(--color-accent)"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="var(--color-accent)"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
            />
          </svg>
          <span
            className="text-sm"
            style={{ color: "var(--color-text-muted)" }}
          >
            Loading...
          </span>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
