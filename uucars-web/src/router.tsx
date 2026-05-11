import { createBrowserRouter } from "react-router-dom";
import LoginPage from "@/pages/LoginPage";
import CreateCarPage from "@/pages/CreateCarPage";
import ProtectedRoute from "@/components/ProtectedRoute";
import RegisterPage from "@/pages/RegisterPage";
import VerifyEmailPage from "@/pages/VerifyEmailPage";
import ForgotPasswordPage from "@/pages/ForgotPasswordPage";
import ResetPasswordPage from "@/pages/ResetPasswordPage";
import Layout from "@/components/Layout";
import HomePage from "@/pages/HomePage";
import CarDetailPage from "@/pages/CarDetailPage";
import EditCarPage from "./pages/EditCarPage";
import ProfilePage from "./pages/ProfilePage";
import MyListingsPage from "./pages/MyListingsPage";
import MyFavoritesPage from "./pages/MyFavoritesPage";
import MyPurchasesPage from "./pages/MyPurchasesPage";
import MySalesPage from "./pages/MySalesPage";

export const router = createBrowserRouter([
  // 认证页面：不需要导航栏（全屏居中布局）
  // 公开路由
  {
    path: "/login",
    element: <LoginPage />,
  },
  {
    path: "/register",
    element: <RegisterPage />,
  },
  {
    path: "/verify-email",
    element: <VerifyEmailPage />,
  },
  {
    path: "/forgot-password",
    element: <ForgotPasswordPage />,
  },
  {
    path: "/reset-password",
    element: <ResetPasswordPage />,
  },

  // 有导航栏的页面：都放在 Layout 里
  {
    element: <Layout />,
    children: [
      // 公开路由
      { path: "/", element: <HomePage /> },
      { path: "/cars/:id", element: <CarDetailPage /> },
      // 受保护路由
      // element 是 ProtectedRoute，它负责权限检查
      // children 里的页面只有登录后才能访问
      {
        element: <ProtectedRoute />,
        children: [
          {
            path: `/cars/new`,
            element: <CreateCarPage />,
          },
          { path: "/cars/:id/edit", element: <EditCarPage /> },

          {
            path: "/profile",
            element: <ProfilePage />,
            children: [
              // index: true 表示访问 /profile 时默认显示这个子路由
              { index: true, element: <MyListingsPage /> },
              { path: "listings", element: <MyListingsPage /> },
              { path: "favorites", element: <MyFavoritesPage /> },
              { path: "purchases", element: <MyPurchasesPage /> },
              { path: "sales", element: <MySalesPage /> },
            ],
          },
        ],
      },
    ],
  },
]);
