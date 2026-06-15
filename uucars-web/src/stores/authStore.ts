import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { User } from "@/types";
import axios from "axios";

interface AuthState {
  user: User | null;
  accessToken: string | null;

  // 新增：是否在做启动时的token恢复
  isInitializing: boolean;

  // 登录成功后调用：保存用户信息和 Token
  setAuth: (user: User, token: string) => void;

  // ✅ 新增：只更新 AccessToken（拦截器刷新后调用）
  // 不影响 user 信息，只替换 Token
  setAccessToken: (token: string) => void;

  // 退出登录：清除所有状态
  clearAuth: () => void;

  // 判断当前是否已登录
  isAuthenticated: () => boolean;

  // 新增：应用启动时调用，尝试用 refresh token 恢复登录态
  initialize: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  // persist 中间件：把 store 的状态同步到 localStorage
  // 页面刷新后状态不会丢失，用户不需要重新登录
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      isInitializing: true, // 初始为true，应用启动时正在恢复

      setAuth: (user, token) => {
        // ✅ 移除：不再把 accessToken 单独存 localStorage
        // 原因：accessToken 短期有效（60分钟），持久化意义不大
        // 且存在 localStorage 里有 XSS 风险（JS 可以读取）
        // 刷新页面后通过 /auth/refresh 重新获取，RefreshToken 在 HttpOnly Cookie 里
        set({ user, accessToken: token });
      },

      // ✅ 新增：拦截器刷新成功后调用，只更新 Token
      setAccessToken: (token) => {
        set({ accessToken: token });
      },

      clearAuth: () => {
        // ✅ 移除：不再需要单独清 localStorage 的 accessToken
        // persist 中间件会自动把 null 同步到 localStorage
        set({ user: null, accessToken: null });
      },

      isAuthenticated: () => {
        return get().accessToken !== null && get().user !== null;
      },

      initialize: async () => {
        const { user } = get();

        // localStorage里没有user信息 → 从未登录过，无需refresh
        if (!user) {
          set({ isInitializing: false });
          return;
        }

        // 有user信息 → 尝试用 refresh token (cookie) 换新的 access token
        try {
          const response = await axios.post(
            `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
            null,
            { withCredentials: true },
          );
          const newAccessToken = response.data.data.accessToken;
          set({ accessToken: newAccessToken, isInitializing: false });
        } catch {
          // refresh token也失效了 → 清除登录态
          set({ user: null, accessToken: null, isInitializing: false });
        }
      },
    }),
    {
      name: "auth-storage", // localStorage 里的 key 名称
      // ✅ 更新：只持久化 user，不持久化 accessToken
      // 原因：
      // 1. accessToken 60分钟过期，持久化后刷新页面拿到的可能是过期 Token
      // 2. 存 localStorage 有 XSS 风险
      // 3. 刷新页面时拦截器会自动用 RefreshToken（HttpOnly Cookie）换新 AccessToken
      //    用户无感知，不需要持久化 accessToken
      partialize: (state) => ({
        user: state.user,
      }),
    },
  ),
);
