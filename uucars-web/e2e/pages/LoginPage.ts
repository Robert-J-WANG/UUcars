// e2e/pages/LoginPage.ts
import { type Page } from "@playwright/test";

export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto() {
    await this.page.goto("/login");
  }

  async login(email: string, password: string) {
    await this.page.getByLabel("Email").fill(email);
    await this.page.getByLabel("Password").fill(password);
    await this.page.getByRole("button", { name: /sign in/i }).click();
  }

  // expectedUrl 默认还是 "/"，普通用户登录不用改调用方式
  // Admin 登录时传 "/admin"，跳过去哪由调用方决定，不由 POM 自己假设
  async loginAndWait(
    email: string,
    password: string,
    expectedUrl: string | RegExp = "/",
  ) {
    await this.goto();
    await this.login(email, password);
    await this.page.waitForURL(expectedUrl);
  }
}
