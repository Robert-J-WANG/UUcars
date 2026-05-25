// src/components/ui/input.tsx
import * as React from "react";
import { cn } from "@/lib/utils";

function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        // 基础：背景、边框、圆角
        "flex h-10 w-full rounded-[var(--radius-md)] border border-[var(--color-border-strong)] bg-white px-3.5 py-2",
        // 文字
        "text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)]",
        // 交互：焦点边框变为主色
        "transition-colors duration-150",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-primary)] focus-visible:ring-offset-0 focus-visible:border-[var(--color-primary)]",
        // 禁用
        "disabled:cursor-not-allowed disabled:opacity-50 disabled:bg-[var(--color-bg)]",
        // 文件上传
        "file:border-0 file:bg-transparent file:text-sm file:font-medium",
        className,
      )}
      {...props}
    />
  );
}

export { Input };
