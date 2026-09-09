import { GoogleLogin, type CredentialResponse } from "@react-oauth/google";
import { toast } from "sonner";
import { authApi } from "@/api";
import type { LoginResponse } from "@/types";

interface GoogleSignInButtonProps {
  // 回调函数：Google 登录成功后，把 UUcars 的登录结果交给页面。
  onAuthenticated: (result: LoginResponse) => void;
}

function GoogleSignInButton({ onAuthenticated }: GoogleSignInButtonProps) {
  const handleSuccess = async (response: CredentialResponse) => {
    // credential 才是需要发送给 UUcars 后端的 Google ID Token。
    if (!response.credential) {
      toast.error("Google did not return an ID token.");
      return;
    }

    try {
      // 后端验证 Google Token，并返回 UUcars 的登录结果。
      const result = await authApi.googleLogin(response.credential);

      // 页面决定成功后如何保存状态和跳转。
      onAuthenticated(result);
    } catch (error) {
      // Axios 拦截器已将后端错误转换为 Error。
      toast.error(
        error instanceof Error ? error.message : "Google sign-in failed.",
      );
    }
  };

  return (
    <GoogleLogin
      type="icon"
      shape="circle"
      size="large"
      theme="outline"
      onSuccess={(response) => {
        void handleSuccess(response);
      }}
      onError={() => {
        // 用户取消弹窗或 Google 页面本身失败时，不会调用后端。
        toast.error("Google sign-in was cancelled or failed.");
      }}
    />
  );
}

export default GoogleSignInButton;
