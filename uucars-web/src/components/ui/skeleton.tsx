import { cn } from "@/lib/utils";

function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="skeleton"
      className={cn(
        "animate-pulse rounded-md bg-[var(--color-primary-light)]",
        className,
      )}
      {...props}
    />
  );
}

export { Skeleton };
