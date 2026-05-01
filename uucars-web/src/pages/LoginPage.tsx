import { useState } from "react";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useNavigate } from "react-router-dom";
import { authApi } from "@/api";
import { useAuthStore } from "@/stores/authStore";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

// 定义验证规则
const loginSchema = z.object({
  email: z.email("Invalid email").min(1, "Email is required"),
  password: z.string().min(1, "Password is required"),
});
// 从 Schema 推导类型
type LoginForm = z.infer<typeof loginSchema>;

export default function LoginPage() {
  // 路由跳转
  const navigate = useNavigate();
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

      // 跳转首页
      navigate("/");
    } catch (error) {
      // Axios 拦截器已经把错误提取成 Error 对象
      // 直接读 message 显示给用户
      if (error instanceof Error) {
        setServerError(error.message);
      }
    }
  };
  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl font-bold">Sign in</CardTitle>
          <CardDescription>
            Enter your email and password to sign in
          </CardDescription>
        </CardHeader>

        <CardContent>
          {/* 把 onSubmit 传给 handleSubmit */}
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {serverError && (
              <div className="rounded-md bg-red-50 p-3 text-sm text-red-600">
                {serverError}
              </div>
            )}

            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              {/* register('email') 展开传给 input，RHF 开始追踪这个字段 */}
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                {...register("email")}
              />
              {errors.email && (
                <p className="text-sm text-red-500">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>

              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-red-500">
                  {errors.password.message}
                </p>
              )}
            </div>

            <div className="flex justify-end">
              <Link
                to="/forgot-password"
                className="text-sm text-blue-600 hover:underline"
              >
                Forgot password?
              </Link>
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? "Signing in..." : "Sign in"}
            </Button>

            <p className="text-center text-sm text-gray-600">
              Don't have an account?{" "}
              <Link to="/register" className="text-blue-600 hover:underline">
                Sign up
              </Link>
            </p>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
