// src/api/client.ts
import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { ApiResponse } from "@/types";
import { toast } from "sonner";
import { useAuthStore } from "@/stores/authStore";

// 创建 Axios 实例
// 所有请求都基于这个实例，统一配置 baseURL 和超时
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 10000, // 10秒超时
  headers: {
    "Content-Type": "application/json",
  },
  // ✅ 新增：允许跨域请求携带 Cookie
  // 原因：RefreshToken 存在 HttpOnly Cookie 里
  // withCredentials=true 告诉浏览器在跨域请求时携带 Cookie
  // 需要后端 CORS 同步配置 AllowCredentials()
  withCredentials: true,
});

// =============================================
// 请求拦截器：自动附加 JWT Token
// =============================================
// 每次请求发出前，从 localStorage 取出 Token 附加到请求头
// 这样所有需要认证的接口不需要手动传 Token
apiClient.interceptors.request.use((config) => {
  // ✅ 从 authStore 读取 accessToken
  // 不再从 localStorage 直接读
  // 原因：accessToken 不再持久化到 localStorage
  // authStore 是唯一数据源，拦截器刷新后也会更新 authStore
  const token = useAuthStore.getState().accessToken;
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

  async (error: AxiosError) => {
    // error.config 包含了失败请求的完整配置（url、method、headers等）
    // 用 as 做类型断言，告诉 TypeScript 这个对象的类型
    // & { _retry?: boolean } 是交叉类型：在 Axios 原有类型基础上，
    // 扩展一个我们自己加的 _retry 字段
    // 原因：InternalAxiosRequestConfig 里没有 _retry，
    // 不声明的话 TypeScript 会报错"该属性不存在"
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      // _retry 标志：防止无限重试
      // 第一次 401 → _retry 为 undefined（falsy）→ 进入刷新逻辑 → 设为 true
      // 刷新后重试原请求 → 如果还是 401 → _retry 为 true → 跳过刷新 → 走普通错误处理
      _retry?: boolean;
    };

    // =============================================
    // 处理 401 Token 过期：自动刷新后重试
    // =============================================
    if (error.response?.status === 401 && !originalRequest._retry) {
      // 标记这个请求已经尝试过刷新
      // 防止刷新后重试的请求再次触发刷新（死循环）
      originalRequest._retry = true;

      try {
        // 调用刷新接口
        // 用原生 axios 而不是 apiClient 的原因：
        // 避免触发 apiClient 自己的拦截器（否则刷新失败又触发401，又来刷新，死循环）
        // withCredentials：携带 HttpOnly Cookie 里的 RefreshToken
        // RefreshToken 由浏览器自动附加，前端 JS 不需要（也无法）手动读取
        const response = await axios.post(
          `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
          null,
          { withCredentials: true },
        );

        // 从响应里取出新的 AccessToken
        // 后端返回格式：{ success: true, data: { accessToken: "..." } }
        const newAccessToken = response.data.data.accessToken;

        // 把新 Token 存入 authStore（内存），供后续请求使用
        // 不存 localStorage 的原因：AccessToken 短期有效，持久化意义不大
        // 且 localStorage 可被 JS 读取，有 XSS 风险
        useAuthStore.getState().setAccessToken(newAccessToken);

        // 用新 Token 更新原请求的 Authorization 头，然后重新发送
        // 对调用方完全透明：await apiClient.get("/cars") 最终正常返回数据
        // 用户不知道中间发生了一次 Token 刷新
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return apiClient(originalRequest);
      } catch {
        // 刷新失败：说明 RefreshToken 也过期了或被撤销（比如已在其他设备登出）
        // 无法继续保持登录状态，必须让用户重新登录

        // 清除前端的用户状态（authStore + localStorage 里的 user）
        useAuthStore.getState().clearAuth();

        // 跳转登录页
        // 带上当前路径（redirect 参数），登录成功后可以回到原来的页面
        window.location.href = `/login?redirect=${encodeURIComponent(window.location.pathname)}`;

        return Promise.reject(new Error("Session expired, please login again"));
      }
    }

    // =============================================
    // 处理 409 Conflict（并发冲突）
    // =============================================
    if (error.response?.status === 409) {
      // 并发冲突：两个操作同时修改了同一条数据
      // 后端用乐观锁（RowVersion）检测到冲突，返回 409
      // 提示用户刷新后重试，不需要任何特殊的恢复逻辑
      const apiResponse = error.response.data as ApiResponse<unknown>;
      const message =
        apiResponse?.message ??
        "This resource was modified by another operation. Please refresh and try again.";

      toast.error(message, { duration: 5000 });

      return Promise.reject(new Error(message));
    }

    // =============================================
    // 处理 429 Too Many Requests（原有逻辑不变）
    // =============================================
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

    // =============================================
    // 处理其他错误（原有逻辑不变）
    // =============================================
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
