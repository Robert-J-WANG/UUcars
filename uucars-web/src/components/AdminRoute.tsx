import { useAuthStore } from "@/stores/authStore";
import { Navigate, Outlet } from "react-router-dom";

export default function AdminRoute() {
  const { user, isAuthenticated } = useAuthStore();

  // 未登录 → 跳登录页
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  // 已登录但不是 Admin → 跳首页
  if (user?.role !== "Admin") {
    return <Navigate to="/404" replace />;
  }

  return <Outlet />;
}
