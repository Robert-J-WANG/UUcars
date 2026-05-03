import { createBrowserRouter } from "react-router-dom";
import LoginPage from "@/pages/LoginPage";
import CreateCarPage from "@/pages/CreateCarPage";
import ProtectedRoute from "@/components/ProtectedRoute";
import RegisterPage from "@/pages/RegisterPage";
import VerifyEmailPage from "@/pages/VerifyEmailPage";
import ForgotPasswordPage from "@/pages/ForgotPasswordPage";
import ResetPasswordPage from "@/pages/ResetPasswordPage";

export const router = createBrowserRouter([
  // 公开路由
  {
    path: "/",
    element: <div className="p-8">首页（占位）</div>,
  },
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

  // 受保护路由
  // element 是 ProtectedRoute，它负责权限检查
  // children 里的页面只有登录后才能访问
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: "/cars/new",
        element: <CreateCarPage />,
      },
    ],
  },
]);
