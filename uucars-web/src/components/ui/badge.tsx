// src/components/ui/badge.tsx
import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors",
  {
    variants: {
      variant: {
        default: "border-transparent bg-[var(--color-primary)] text-white",
        secondary:
          "border-transparent bg-[var(--color-primary-light)] text-[var(--color-primary)] border-[var(--color-primary-light)]",
        accent:
          "border-transparent bg-[var(--color-accent-light)] text-[var(--color-accent-hover)]",
        outline:
          "border-[var(--color-border-strong)] text-[var(--color-text-secondary)] bg-transparent",
        success:
          "border-transparent bg-[var(--color-success-light)] text-[var(--color-success)]",
        destructive:
          "border-transparent bg-[var(--color-danger-light)] text-[var(--color-danger)]",
        warning:
          "border-transparent bg-[var(--color-warning-light)] text-[var(--color-warning)]",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

export interface BadgeProps
  extends
    React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}

export { Badge, badgeVariants };
