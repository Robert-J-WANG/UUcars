import { useState } from "react";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { authApi } from "@/api";
import { useAuthStore } from "@/stores/authStore";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Car } from "lucide-react";

// 定义验证规则
const loginSchema = z.object({
  email: z.email("Invalid email").min(1, "Email is required"),
  password: z.string().min(1, "Password is required"),
});
// 从 Schema 推导类型
type LoginForm = z.infer<typeof loginSchema>;

// 定义location state数据类型
interface LocationState {
  from?: { pathname: string };
}

export default function LoginPage() {
  // 路由跳转
  const navigate = useNavigate();
  // 路由来源
  const loction = useLocation();
  const { setAuth } = useAuthStore();
  // const [isLoading, setIsLoading] = useState<boolean>(false);
  // 服务端错误信息（密码错误、邮箱未验证等）
  const [serverError, setServerError] = useState<string | null>(null);

  // 初始化 RHF，连接 Zod
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  // 获取路由跳转来源页路径信息
  const state = loction.state as LocationState | null;
  const from = state?.from?.pathname;

  // onSubmit 只在 RHF 验证通过后才会被调用
  // data 是表单所有字段的值，类型是 LoginForm
  const onSubmit = async (data: LoginForm) => {
    // 清除上次的服务端错误
    setServerError(null);

    try {
      // 调用登录 API
      const result = await authApi.login(data);

      // 登录成功：把用户信息和 Token 存入 Zustand
      // setAuth 内部会同时写入 localStorage
      setAuth(result.user, result.token);

      // 跳转
      // 有来源页就跳回去，没有的话 Admin 跳管理页、普通用户跳首页
      if (from) {
        navigate(from, { replace: true });
      } else if (result.user.role === "Admin") {
        navigate("/admin", { replace: true });
      } else {
        navigate("/", { replace: true });
      }
    } catch (error) {
      // Axios 拦截器已经把错误提取成 Error 对象
      // 直接读 message 显示给用户
      if (error instanceof Error) {
        setServerError(error.message);
      }
    }
  };
  return (
    <div
      className="flex min-h-screen items-center justify-center px-4 py-12"
      style={{ backgroundColor: "var(--color-bg)" }}
    >
      {/* 背景装饰 */}
      <div
        className="pointer-events-none fixed inset-0 opacity-30"
        style={{
          background:
            "radial-gradient(ellipse 80% 60% at 50% -10%, var(--color-primary-light), transparent)",
        }}
      />

      <div className="relative w-full max-w-sm animate-fade-in-up">
        {/* Logo */}
        <div className="mb-8 text-center">
          <Link to={"/"} className="mb-3 inline-flex items-center gap-2">
            <div
              className="flex h-9 w-9 items-center justify-center rounded-xl"
              style={{ backgroundColor: "var(--color-accent)" }}
            >
              <Car className="h-5 w-5 text-white" />
            </div>
            <span
              className="text-xl font-bold"
              style={{
                color: "var(--color-accent)",
                fontFamily: "'DM Serif Display', serif",
              }}
            >
              UUcars
            </span>
          </Link>
          <h1
            className="text-2xl"
            style={{
              color: "var(--color-text-primary)",
              fontFamily: "'DM Serif Display', serif",
            }}
          >
            Welcome back
          </h1>
          <p
            className="mt-1 text-sm"
            style={{ color: "var(--color-text-secondary)" }}
          >
            Sign in to your account
          </p>
        </div>

        {/* 表单卡片 */}
        <div
          className="rounded-[var(--radius-xl)] border p-7"
          style={{
            backgroundColor: "var(--color-surface)",
            borderColor: "var(--color-border)",
            boxShadow: "var(--shadow-lg)",
          }}
        >
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* 服务端错误 */}
            {serverError && (
              <div
                className="rounded-[var(--radius-md)] px-4 py-3 text-sm"
                style={{
                  backgroundColor: "var(--color-danger-light)",
                  color: "var(--color-danger)",
                  border: "1px solid",
                  borderColor:
                    "color-mix(in srgb, var(--color-danger) 20%, transparent)",
                }}
              >
                {serverError}
              </div>
            )}

            <div className="space-y-1.5">
              <label
                htmlFor="email"
                className="text-sm font-medium"
                style={{ color: "var(--color-text-primary)" }}
              >
                Email
              </label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                autoComplete="email"
                {...register("email")}
              />
              {errors.email && (
                <p className="text-xs" style={{ color: "var(--color-danger)" }}>
                  {errors.email.message}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label
                  htmlFor="password"
                  className="text-sm font-medium"
                  style={{ color: "var(--color-text-primary)" }}
                >
                  Password
                </label>
                <Link
                  to="/forgot-password"
                  className="text-xs transition-colors hover:underline"
                  style={{ color: "var(--color-primary)" }}
                >
                  Forgot password?
                </Link>
              </div>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                autoComplete="current-password"
                {...register("password")}
              />
              {errors.password && (
                <p className="text-xs" style={{ color: "var(--color-danger)" }}>
                  {errors.password.message}
                </p>
              )}
            </div>

            <Button
              type="submit"
              className="w-full"
              size="lg"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <span className="flex items-center gap-2">
                  <svg
                    className="h-4 w-4 animate-spin"
                    viewBox="0 0 24 24"
                    fill="none"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                    />
                  </svg>
                  Signing in...
                </span>
              ) : (
                "Sign in"
              )}
            </Button>
          </form>
        </div>

        <p
          className="mt-5 text-center text-sm"
          style={{ color: "var(--color-text-secondary)" }}
        >
          Don't have an account?{" "}
          <Link
            to="/register"
            className="font-medium transition-colors hover:underline"
            style={{ color: "var(--color-accent)" }}
          >
            Create one free
          </Link>
        </p>
      </div>
    </div>
  );
}
