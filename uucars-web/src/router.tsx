// src/router.tsx
import { Suspense, type ReactNode } from "react";
import RouteLoading from "@/components/RouteLoading";

import { createBrowserRouter, Navigate } from "react-router-dom";
import ProtectedRoute from "@/components/ProtectedRoute";
import AdminRoute from "@/components/AdminRoute";

// 认证页面
import VerifyEmailPage from "@/pages/VerifyEmailPage";
import ForgotPasswordPage from "@/pages/ForgotPasswordPage";
import ResetPasswordPage from "@/pages/ResetPasswordPage";

// 公开页面
import HomePage from "@/pages/HomePage";
import CarDetailPage from "@/pages/CarDetailPage";
import NotFoundPage from "@/pages/NotFoundPage";

// 需要登录的页面
import ProfilePage from "@/pages/ProfilePage";
import MyListingsPage from "@/pages/MyListingsPage";
import MyFavoritesPage from "@/pages/MyFavoritesPage";
import MyPurchasesPage from "@/pages/MyPurchasesPage";
import MySalesPage from "@/pages/MySalesPage";

// Admin 页面

import Layout from "./components/Layout";
import ErrorBoundary from "@/components/ErrorBoundary";

import {
  AdminDashboardPage,
  AdminPage,
  AdminPendingPage,
  CreateCarPage,
  EditCarPage,
  GoogleAuthLayout,
  LoginPage,
  RegisterPage,
} from "@/routes/lazyRouteComponents";

const withRouteBoundary = (element: ReactNode) => (
  <ErrorBoundary>
    <Suspense fallback={<RouteLoading />}>{element}</Suspense>
  </ErrorBoundary>
);

export const router = createBrowserRouter([
  // =============================================
  // 认证页面：不需要导航栏（全屏居中布局）
  // =============================================
  {
    element: withRouteBoundary(<GoogleAuthLayout />),
    children: [
      { path: "/login", element: withRouteBoundary(<LoginPage />) },
      { path: "/register", element: withRouteBoundary(<RegisterPage />) },
    ],
  },
  { path: "/verify-email", element: withRouteBoundary(<VerifyEmailPage />) },
  {
    path: "/forgot-password",
    element: withRouteBoundary(<ForgotPasswordPage />),
  },
  {
    path: "/reset-password",
    element: withRouteBoundary(<ResetPasswordPage />),
  },

  // =============================================
  // 有导航栏的页面：都放在 Layout 里
  // =============================================
  {
    element: <Layout />,
    children: [
      // =============================================
      // 公开路由（无需登录）
      // =============================================
      { path: "/", element: withRouteBoundary(<HomePage />) },
      { path: "/cars/:id", element: withRouteBoundary(<CarDetailPage />) },

      // =============================================
      // 受保护路由（需要登录）
      // 用 ProtectedRoute 作为父路由
      // children 里的页面只有登录后才能访问
      // =============================================
      {
        element: <ProtectedRoute />,
        children: [
          { path: "/cars/new", element: withRouteBoundary(<CreateCarPage />) },
          {
            path: "/cars/:id/edit",
            element: withRouteBoundary(<EditCarPage />),
          },

          // 个人中心：嵌套路由
          // ProfilePage 里用 Outlet 渲染子路由内容
          // 访问 /profile 时渲染 ProfilePage + MyListingsPage（默认子路由）
          {
            path: "/profile",
            element: withRouteBoundary(<ProfilePage />),
            children: [
              { index: true, element: withRouteBoundary(<MyListingsPage />) },
              {
                path: "listings",
                element: withRouteBoundary(<MyListingsPage />),
              },
              {
                path: "favorites",
                element: withRouteBoundary(<MyFavoritesPage />),
              },
              {
                path: "purchases",
                element: withRouteBoundary(<MyPurchasesPage />),
              },
              { path: "sales", element: withRouteBoundary(<MySalesPage />) },
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
            element: withRouteBoundary(<AdminPage />),
            children: [
              {
                index: true,
                element: <Navigate to="dashboard" replace />,
              },
              {
                path: "dashboard",
                element: withRouteBoundary(<AdminDashboardPage />),
              },
              {
                path: "pending",
                element: withRouteBoundary(<AdminPendingPage />),
              },
            ],
          },
        ],
      },
    ],
  },

  // 404
  { path: "/404", element: withRouteBoundary(<NotFoundPage />) },
  { path: "*", element: withRouteBoundary(<NotFoundPage />) },
]);
