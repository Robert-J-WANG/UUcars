import { useEffect, useRef, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { authApi } from "@/api";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

type VerifyStatus = "loading" | "success" | "error";

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState<VerifyStatus>("loading");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const hasAlreadyVerified = useRef(false);

  useEffect(() => {
    if (hasAlreadyVerified.current) return;
    hasAlreadyVerified.current = true;
    const verify = async () => {
      const token = searchParams.get("token");
      if (!token) {
        setStatus("error");
        setErrorMessage("Invalid verification link.");
        return;
      }

      try {
        await authApi.verifyEmail(token);
        setStatus("success");
      } catch (error) {
        setStatus("error");
        if (error instanceof Error) {
          setErrorMessage(error.message);
        }
      }
    };

    verify();
  }, [searchParams]);
  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <Card className="w-full max-w-md">
        {status === "loading" && (
          <CardHeader>
            <CardTitle>Verifying your email...</CardTitle>
            <CardDescription>Please wait a moment.</CardDescription>
          </CardHeader>
        )}

        {status === "success" && (
          <>
            <CardHeader>
              <CardTitle>Email verified!</CardTitle>
              <CardDescription>
                Your account is now active. You can sign in.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {/*
                Button asChild 是 shadcn 的特殊用法：
                把 Button 的样式应用到子元素（Link）上
                而不是渲染一个 <button> 标签包着 <a> 标签
                （button 包 a 是不合法的 HTML 写法）
              */}
              <Button asChild className="w-full">
                <Link to="/login">Sign in</Link>
              </Button>
            </CardContent>
          </>
        )}

        {status === "error" && (
          <>
            <CardHeader>
              <CardTitle>Verification failed</CardTitle>
              <CardDescription className="text-red-500">
                {errorMessage ?? "The link is invalid or has expired."}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button asChild variant="outline" className="w-full">
                <Link to="/login">Back to sign in</Link>
              </Button>
            </CardContent>
          </>
        )}
      </Card>
    </div>
  );
}
