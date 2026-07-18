import { defineConfig } from "@playwright/test";

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
    baseURL: "http://localhost:5173",

    // 失败时自动截图，保存在 test-results/ 目录
    // 截图是 E2E 调试的关键工具——失败时能看到浏览器当时的状态
    screenshot: "only-on-failure",

    trace: "on-first-retry",
  },

  projects: [
    {
      name: "chromium",
      use: { browserName: "chromium" },
    },
  ],
});
