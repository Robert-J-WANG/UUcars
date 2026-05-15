import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "@/stores/authStore";

// Outlet 是 React Router 的占位符
// 当路由匹配时，子路由的内容会渲染在 Outlet 的位置
// 把 ProtectedRoute 作为父路由，子路由作为 children
// 用户未登录时渲染 Navigate（跳转），登录了渲染 Outlet（子路由内容）
export default function ProtectedRoute() {
  const { isAuthenticated } = useAuthStore();

  if (!isAuthenticated()) {
    // replace：用登录页替换当前历史记录
    // 这样登录后按返回键不会回到被拦截的页面
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
