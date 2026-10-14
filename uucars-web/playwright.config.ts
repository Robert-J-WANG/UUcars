import { defineConfig } from "@playwright/test";
import { E2E_API_BASE_URL, E2E_FRONTEND_BASE_URL } from "./e2e/config";

export default defineConfig({
  testDir: "./e2e",

  // 生成 HTML 测试报告，跑完后可以用 npx playwright show-report 查看
  reporter: "html",

  // 串行执行，不并行
  // 原因：多个测试共用 Admin 账号，并行时会产生状态冲突
  // （比如测试3审核了测试4准备的车辆，导致测试4流程出错）
  fullyParallel: false,
  workers: 1,

  // CI 环境下禁止 only（防止忘记移除 test.only 导致其他测试没跑）
  forbidOnly: !!process.env.CI,

  // 失败时不重试（重试会掩盖不稳定的测试）
  retries: 0,

  // 单个测试的超时时间：30秒
  timeout: 30000,

  use: {
    baseURL: E2E_FRONTEND_BASE_URL,

    // 失败时自动截图，保存在 test-results/ 目录
    // 截图是 E2E 调试的关键工具——失败时能看到浏览器当时的状态
    screenshot: "only-on-failure",

    trace: "on-first-retry",
  },

  webServer: [
    {
      name: "E2E API",
      cwd: "..",
      command:
        "dotnet run --project UUcars.API/UUcars.API.csproj  --launch-profile e2e",
      url: `${E2E_API_BASE_URL}/cars?page=1&pageSize=1`,
      reuseExistingServer: false,
      timeout: 120_000,
      gracefulShutdown: {
        signal: "SIGINT",
        timeout: 5_000,
      },
    },
    {
      name: "Vite Frontend",
      command: "npm run dev -- --port 5173 --strictPort",
      url: E2E_FRONTEND_BASE_URL,
      env: {
        VITE_API_BASE_URL: E2E_API_BASE_URL,
      },
      reuseExistingServer: false,
      timeout: 120_000,
      gracefulShutdown: {
        signal: "SIGINT",
        timeout: 5_000,
      },
    },
  ],

  projects: [
    {
      name: "chromium",
      use: { browserName: "chromium" },
    },
  ],
});
