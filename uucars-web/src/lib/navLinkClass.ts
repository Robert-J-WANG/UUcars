// src/lib/navLinkClass.ts
import { cn } from "@/lib/utils";

export const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    "text-sm font-medium transition-colors duration-150 relative pb-0.5",
    "after:absolute after:bottom-0 after:left-0 after:h-0.5 after:w-full after:rounded-full",
    "after:transition-transform after:duration-200 after:origin-left",
    isActive
      ? "text-[var(--color-primary)] after:bg-[var(--color-primary)] after:scale-x-100"
      : "text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] after:scale-x-0 hover:after:scale-x-100 after:bg-[var(--color-border-strong)]",
  );
