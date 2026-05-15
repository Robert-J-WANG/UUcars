// src/router.tsx
import { createBrowserRouter } from "react-router-dom";
import ProtectedRoute from "@/components/ProtectedRoute";
import AdminRoute from "@/components/AdminRoute";

// 认证页面
import LoginPage from "@/pages/LoginPage";
import RegisterPage from "@/pages/RegisterPage";
import VerifyEmailPage from "@/pages/VerifyEmailPage";
import ForgotPasswordPage from "@/pages/ForgotPasswordPage";
import ResetPasswordPage from "@/pages/ResetPasswordPage";

// 公开页面
import HomePage from "@/pages/HomePage";
import CarDetailPage from "@/pages/CarDetailPage";
import NotFoundPage from "@/pages/NotFoundPage";

// 需要登录的页面
import CreateCarPage from "@/pages/CreateCarPage";
import EditCarPage from "@/pages/EditCarPage";
import ProfilePage from "@/pages/ProfilePage";
import MyListingsPage from "@/pages/MyListingsPage";
import MyFavoritesPage from "@/pages/MyFavoritesPage";
import MyPurchasesPage from "@/pages/MyPurchasesPage";
import MySalesPage from "@/pages/MySalesPage";

// Admin 页面
import AdminPage from "@/pages/AdminPage";
import AdminPendingPage from "@/pages/AdminPendingPage";
import Layout from "./components/Layout";

export const router = createBrowserRouter([
  // =============================================
  // 认证页面：不需要导航栏（全屏居中布局）
  // =============================================
  { path: "/login", element: <LoginPage /> },
  { path: "/register", element: <RegisterPage /> },
  { path: "/verify-email", element: <VerifyEmailPage /> },
  { path: "/forgot-password", element: <ForgotPasswordPage /> },
  { path: "/reset-password", element: <ResetPasswordPage /> },

  // =============================================
  // 有导航栏的页面：都放在 Layout 里
  // =============================================
  {
    element: <Layout />,
    children: [
      // =============================================
      // 公开路由（无需登录）
      // =============================================
      { path: "/", element: <HomePage /> },
      { path: "/cars/:id", element: <CarDetailPage /> },

      // =============================================
      // 受保护路由（需要登录）
      // 用 ProtectedRoute 作为父路由
      // children 里的页面只有登录后才能访问
      // =============================================
      {
        element: <ProtectedRoute />,
        children: [
          { path: "/cars/new", element: <CreateCarPage /> },
          { path: "/cars/:id/edit", element: <EditCarPage /> },

          // 个人中心：嵌套路由
          // ProfilePage 里用 Outlet 渲染子路由内容
          // 访问 /profile 时渲染 ProfilePage + MyListingsPage（默认子路由）
          {
            path: "/profile",
            element: <ProfilePage />,
            children: [
              { index: true, element: <MyListingsPage /> },
              { path: "listings", element: <MyListingsPage /> },
              { path: "favorites", element: <MyFavoritesPage /> },
              { path: "purchases", element: <MyPurchasesPage /> },
              { path: "sales", element: <MySalesPage /> },
            ],
          },
        ],
      },

      // =============================================
      // Admin 路由（需要 Admin 角色）
      // =============================================
      {
        element: <AdminRoute />,
        children: [
          {
            path: "/admin",
            element: <AdminPage />,
            children: [
              { index: true, element: <AdminPendingPage /> },
              { path: "pending", element: <AdminPendingPage /> },
            ],
          },
        ],
      },
    ],
  },

  // 404
  { path: "/404", element: <NotFoundPage /> },
  { path: "*", element: <NotFoundPage /> },
]);
