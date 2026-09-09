import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link, useNavigate } from "react-router-dom";
import { authApi } from "@/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import PasswordInput from "@/components/PasswordInput";
import { Car } from "lucide-react";
import GoogleSignInButton from "@/components/GoogleSignInButton";
import { useAuthStore } from "@/stores/authStore";
import type { LoginResponse } from "@/types";

const registerSchema = z.object({
  username: z
    .string()
    .min(2, "Username must be at least 2 characters")
    .max(50, "Username must not exceed 50 characters"),
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email"),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters")
    .regex(/(?=.*[a-z])/, "Must contain at least one lowercase letter")
    .regex(/(?=.*[A-Z])/, "Must contain at least one uppercase letter")
    .regex(/(?=.*\d)/, "Must contain at least one number"),
});

type RegisterForm = z.infer<typeof registerSchema>;

function RegisterPage() {
  const [serverError, setServerError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  });

  const completeGoogleAuthentication = (result: LoginResponse) => {
    // Google 登录已经得到 UUcars User 和 Access Token。
    setAuth(result.user, result.token);

    // RegisterPage 没有来源页，成功后直接进入首页。
    navigate("/", { replace: true });
  };

  const onSubmit = async (data: RegisterForm) => {
    setServerError(null);
    try {
      await authApi.register(data);
      // 注册成功：显示"请检查邮箱"提示
      setIsSuccess(true);
    } catch (error) {
      if (error instanceof Error) {
        setServerError(error.message);
      }
    }
  };
  // 注册成功后显示提示
  if (isSuccess) {
    console.log(isSuccess);
    return (
      <div
        className="flex min-h-screen items-center justify-center px-4 py-12"
        style={{ backgroundColor: "var(--color-bg)" }}
      >
        <div
          className="pointer-events-none fixed inset-0 opacity-30"
          style={{
            background:
              "radial-gradient(ellipse 80% 60% at 50% -10%, var(--color-primary-light), transparent)",
          }}
        />

        <div className="relative w-full max-w-sm animate-fade-in-up">
          <div className="mb-8 text-center">
            <Link to="/" className="mb-3 inline-flex items-center gap-2">
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
              Check your email
            </h1>
            <p
              className="mt-1 text-sm"
              style={{ color: "var(--color-text-secondary)" }}
            >
              One more step to activate your account
            </p>
          </div>

          <div
            className="rounded-[var(--radius-xl)] border p-7"
            style={{
              backgroundColor: "var(--color-surface)",
              borderColor: "var(--color-border)",
              boxShadow: "var(--shadow-lg)",
            }}
          >
            <p
              className="mb-6 text-center text-sm leading-6"
              style={{ color: "var(--color-text-secondary)" }}
            >
              We've sent a verification link to your email. Click the link to
              activate your account.
            </p>

            <Button
              variant="outline"
              size="lg"
              className="w-full"
              onClick={() => navigate("/login")}
            >
              Back to sign in
            </Button>
          </div>
        </div>
      </div>
    );
  }

  // 注册表单
  return (
    <div
      className="flex min-h-screen items-center justify-center px-4 py-12"
      style={{ backgroundColor: "var(--color-bg)" }}
    >
      <div
        className="pointer-events-none fixed inset-0 opacity-30"
        style={{
          background:
            "radial-gradient(ellipse 80% 60% at 50% -10%, var(--color-primary-light), transparent)",
        }}
      />

      <div className="relative w-full max-w-sm animate-fade-in-up">
        <div className="mb-8 text-center">
          <Link to="/" className="mb-3 inline-flex items-center gap-2">
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
            Create account
          </h1>
          <p
            className="mt-1 text-sm"
            style={{ color: "var(--color-text-secondary)" }}
          >
            Join UUcars to buy and sell cars
          </p>
        </div>

        <div
          className="rounded-[var(--radius-xl)] border p-7"
          style={{
            backgroundColor: "var(--color-surface)",
            borderColor: "var(--color-border)",
            boxShadow: "var(--shadow-lg)",
          }}
        >
          <form
            onSubmit={handleSubmit(onSubmit)}
            noValidate
            className="space-y-5"
          >
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
              <Label
                htmlFor="username"
                style={{ color: "var(--color-text-primary)" }}
              >
                Username
              </Label>
              <Input
                id="username"
                placeholder="johndoe"
                autoComplete="username"
                {...register("username")}
              />
              {errors.username && (
                <p
                  className="text-xs"
                  style={{ color: "var(--color-danger)" }}
                >
                  {errors.username.message}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="email"
                style={{ color: "var(--color-text-primary)" }}
              >
                Email
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                autoComplete="email"
                {...register("email")}
              />
              {errors.email && (
                <p
                  className="text-xs"
                  style={{ color: "var(--color-danger)" }}
                >
                  {errors.email.message}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="password"
                style={{ color: "var(--color-text-primary)" }}
              >
                Password
              </Label>
              <PasswordInput
                id="password"
                placeholder="••••••••"
                autoComplete="new-password"
                {...register("password")}
              />
              {errors.password && (
                <p
                  className="text-xs"
                  style={{ color: "var(--color-danger)" }}
                >
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
              {isSubmitting ? "Creating account..." : "Create account"}
            </Button>
          </form>

          {/* 分割线 */}
          <div className="my-6 flex items-center gap-3">
            <div className="h-px flex-1 bg-[var(--color-border-strong)]" />

            <span className="shrink-0 text-sm text-muted-foreground">
              Or continue with
            </span>

            <div className="h-px flex-1 bg-[var(--color-border-strong)]" />
          </div>

          {/* Google登录 */}
          <div className="flex justify-center">
            <GoogleSignInButton
              onAuthenticated={completeGoogleAuthentication}
            />
          </div>
        </div>

        <p
          className="mt-5 text-center text-sm"
          style={{ color: "var(--color-text-secondary)" }}
        >
          Already have an account?{" "}
          <Link
            to="/login"
            className="font-medium transition-colors hover:underline"
            style={{ color: "var(--color-accent)" }}
          >
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}

export default RegisterPage;
