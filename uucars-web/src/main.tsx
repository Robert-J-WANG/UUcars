import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { router } from "./router";
import { Toaster } from "sonner";
import "./index.css";

// 创建 QueryClient 实例
// 所有查询的缓存都存在这个实例里
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // staleTime：数据多久后被认为"过时"
      // 过时之前，重新访问页面不会重新请求，直接用缓存
      // 设为 1 分钟：1 分钟内数据被认为是新鲜的
      staleTime: 1000 * 60,

      // retry：请求失败时重试次数，默认 3 次，改为 1 次
      retry: 1,
    },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    {/* QueryClientProvider 必须包在 RouterProvider 外面 */}
    {/* 这样路由里的所有组件都能使用 TanStack Query */}
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      {/* Toaster 放在最外层，所有页面都能用 */}
      <Toaster position="bottom-right" />
    </QueryClientProvider>
  </StrictMode>,
);
