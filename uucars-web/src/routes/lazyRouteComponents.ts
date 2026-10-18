import { lazy } from "react";

export const CreateCarPage = lazy(() => import("@/pages/CreateCarPage"));
export const EditCarPage = lazy(() => import("@/pages/EditCarPage"));

export const AdminPage = lazy(() => import("@/pages/AdminPage"));
export const AdminDashboardPage = lazy(
  () => import("@/pages/AdminDashboardPage"),
);
export const AdminPendingPage = lazy(() => import("@/pages/AdminPendingPage"));

export const GoogleAuthLayout = lazy(
  () => import("@/components/GoogleAuthLayout"),
);
export const LoginPage = lazy(() => import("@/pages/LoginPage"));
export const RegisterPage = lazy(() => import("@/pages/RegisterPage"));
