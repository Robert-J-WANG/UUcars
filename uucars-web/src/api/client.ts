// src/api/client.ts
import axios from "axios";
import type { ApiResponse } from "@/types";

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
  (error) => {
    // 请求失败（HTTP 4xx / 5xx / 网络错误）
    if (error.response) {
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
