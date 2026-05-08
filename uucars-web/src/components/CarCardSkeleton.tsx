import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

export default function CarCardSkeleton() {
  return (
    <Card>
      <CardHeader className="pb-2">
        {/* Skeleton 组件：显示灰色闪烁的占位块 */}
        {/* className 控制它的大小，和真实内容的大小保持一致 */}
        <Skeleton className="h-5 w-3/4" />
        <Skeleton className="h-5 w-1/4" />
      </CardHeader>
      <CardContent className="space-y-2">
        <Skeleton className="h-8 w-1/3" />
        <Skeleton className="h-4 w-1/2" />
      </CardContent>
    </Card>
  );
}
