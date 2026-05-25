// src/components/ui/button.tsx
import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-[var(--radius-md)] text-sm font-medium transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-primary)] focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-40 [&_svg]:pointer-events-none [&_svg:not([class*='size-'])]:size-4 shrink-0",
  {
    variants: {
      variant: {
        // ✅ 确保白色文字
        default:
          "bg-[var(--color-primary)] text-white shadow-sm hover:bg-[var(--color-primary-hover)] active:scale-[0.98]",
        accent:
          "bg-[var(--color-accent)] text-white shadow-sm hover:bg-[var(--color-accent-hover)] active:scale-[0.98]",
        destructive:
          "bg-[var(--color-danger)] text-white shadow-sm hover:bg-red-700 active:scale-[0.98]",
        // ✅ hover 改为浅蓝色背景，对比明显
        outline:
          "border border-[var(--color-border-strong)] bg-white text-[var(--color-text-primary)] shadow-[var(--shadow-sm)] hover:bg-[var(--color-primary-light)] hover:text-[var(--color-primary)] hover:border-[var(--color-primary)] active:scale-[0.98]",
        ghost:
          "text-[var(--color-text-secondary)] hover:bg-[var(--color-primary-light)] hover:text-[var(--color-primary)]",
        link: "text-[var(--color-primary)] underline-offset-4 hover:underline p-0 h-auto",
      },
      size: {
        default: "h-10 px-5 py-2",
        sm: "h-8 rounded-[var(--radius-sm)] px-3 text-xs",
        lg: "h-12 rounded-[var(--radius-lg)] px-8 text-base",
        icon: "size-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export interface ButtonProps
  extends
    React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button";
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    );
  },
);
Button.displayName = "Button";

export { Button, buttonVariants };
