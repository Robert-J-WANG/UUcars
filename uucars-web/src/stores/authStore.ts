import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { User } from "@/types";

interface AuthState {
  user: User | null;
  accessToken: string | null;

  // 登录成功后调用：保存用户信息和 Token
  setAuth: (user: User, token: string) => void;

  // 退出登录：清除所有状态
  logout: () => void;

  // 判断当前是否已登录
  isAuthenticated: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  // persist 中间件：把 store 的状态同步到 localStorage
  // 页面刷新后状态不会丢失，用户不需要重新登录
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,

      setAuth: (user, token) => {
        // 同时更新 Zustand 状态和 localStorage
        // localStorage 里的 accessToken 供 Axios 拦截器读取
        localStorage.setItem("accessToken", token);
        set({ user, accessToken: token });
      },

      logout: () => {
        localStorage.removeItem("accessToken");
        set({ user: null, accessToken: null });
      },

      isAuthenticated: () => {
        return get().accessToken !== null && get().user !== null;
      },
    }),
    {
      name: "auth-storage", // localStorage 里的 key 名称
      // 只持久化这两个字段，方法不需要持久化
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
      }),
    }
  )
);
