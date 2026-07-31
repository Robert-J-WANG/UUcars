import { configDefaults, defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  // ✅ 新增：vitest 配置
  test: {
    // environment: "jsdom" 告诉 vitest 用 jsdom 模拟浏览器环境
    // 不设置的话默认是 "node"，没有 document/window，组件渲染会直接报错
    environment: "jsdom",
    // 用 Vitest 的 `setupFiles` 统一处理 导入jest-dom库
    setupFiles: ["./src/test/setup.ts"],
    // globals: true 允许测试文件里直接用 describe/it/expect
    // 不用每个文件手动 import { describe, it, expect } from "vitest"
    globals: true,
    // Playwright E2E 测试由 Playwright 自己运行，不能交给 Vitest
    exclude: [...configDefaults.exclude, "e2e/**"],
  },
});
