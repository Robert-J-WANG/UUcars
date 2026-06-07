// src/api/client.ts
import axios, { AxiosError } from "axios";
import type { ApiResponse } from "@/types";
import { toast } from "sonner";

// 创建 Axios 实例
// 所有请求都基于这个实例，统一配置 baseURL 和超时
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 10000, // 10秒超时
  headers: {
    "Content-Type": "application/json",
  },
});

// =============================================
// 请求拦截器：自动附加 JWT Token
// =============================================
// 每次请求发出前，从 localStorage 取出 Token 附加到请求头
// 这样所有需要认证的接口不需要手动传 Token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// =============================================
// 响应拦截器：统一处理响应和错误
// =============================================
apiClient.interceptors.response.use(
  (response) => {
    // 204 No Content：没有响应体（DELETE 接口），直接放行
    if (response.status === 204) {
      return response;
    }

    // 请求成功（HTTP 2xx）
    // 后端所有接口都用 ApiResponse<T> 包装
    // 直接解包返回 data 字段，调用方不需要每次都写 response.data.data
    const apiResponse = response.data as ApiResponse<unknown>;

    if (!apiResponse.success) {
      // HTTP 状态码是 2xx，但业务逻辑失败（success: false）
      // 把业务错误信息包装成 Error 抛出，调用方统一用 catch 处理
      return Promise.reject(new Error(apiResponse.message ?? "Request failed"));
    }

    return response;
  },
  (error: AxiosError) => {
    // ✅ 处理 429 Too Many Requests
    if (error.response?.status === 429) {
      // 读取 Retry-After 响应头（后端返回的秒数）
      const retryAfterHeader = error.response.headers["retry-after"];
      const seconds = retryAfterHeader ? parseInt(retryAfterHeader, 10) : 60; // 没有 Retry-After 头就默认提示60秒

      // 用 sonner toast 给用户友好提示
      toast.error(
        `Too many requests. Please wait ${seconds} seconds before trying again.`,
        { duration: 5000 },
      );

      // 返回一个标准格式的错误，让调用方知道是限流问题
      return Promise.reject(
        new Error(`Rate limited. Retry after ${seconds}s.`),
      );
    }

    // 处理用户 Token 过期时
    if (error.response?.status === 401) {
      localStorage.removeItem("accessToken");
      // 跳转登录页，带上当前路径方便登录后回跳
      window.location.href = `/login?redirect=${encodeURIComponent(window.location.pathname)}`;
      return Promise.reject(new Error("Session expired, please login again"));
    }

    if (error.response) {
      // 请求失败（HTTP 4xx / 5xx / 网络错误）
      // 服务端返回了错误响应
      const apiResponse = error.response.data as ApiResponse<unknown>;
      const message = apiResponse?.message ?? "An error occurred";
      return Promise.reject(new Error(message));
    }

    if (error.code === "ECONNABORTED") {
      return Promise.reject(new Error("Request timeout, please try again"));
    }

    // 网络错误（断网等）
    return Promise.reject(
      new Error("Network error, please check your connection"),
    );
  },
);

export default apiClient;
