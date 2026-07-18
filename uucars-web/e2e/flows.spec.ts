import { test, expect } from "@playwright/test";
import { LoginPage } from "./pages/LoginPage";
import { createAndVerifyUser, loginAndGetToken } from "./helpers/api";

const ADMIN_EMAIL = "admin@uucars.com";
const ADMIN_PASSWORD = "Admin@123456";
const API = "http://localhost:5065";

test.describe("核心用户流程", () => {
  // ── 测试1：注册流程 ────────────
  test("测试1：用户注册并收到验证提示", async ({ page }) => {
    const uid = Date.now().toString().slice(-6);
    const email = `test-reg-${uid}@example.com`;

    await page.goto("/register");

    await page.getByLabel("Username").fill(`user${uid}`);
    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password").fill("Test@123456");
    await page.getByRole("button", { name: /create account/i }).click();

    // 注册成功后应该显示"请检查邮箱"的提示
    await expect(page.getByText(/check your email/i)).toBeVisible({
      timeout: 10000,
    });
  });

  test("测试2：卖家发布车辆并提交审核", async ({ page, request }) => {
    const uid = Date.now().toString().slice(-6);
    const email = `test-seller-${uid}@example.com`;
    await createAndVerifyUser(request, email);

    const loginPage = new LoginPage(page);
    await loginPage.loginAndWait(email, "Test@123456");

    await page.goto("/cars/new");

    // 标题拼上 uid，避免多次运行产生标题完全相同的重复数据
    await page.getByLabel("Title").fill(`2020 Toyota Corolla Test Car ${uid}`);
    await page.getByLabel("Brand").fill("Toyota");
    await page.getByLabel("Model").fill("Corolla");
    await page.getByLabel("Year").fill("2020");
    await page.getByLabel("Price ($)").fill("18000");
    await page.getByLabel("Mileage (km)").fill("35000");

    await page.getByRole("button", { name: /create draft/i }).click();

    // 草稿创建成功后跳转到编辑页，顺手从 URL 里取出真实的 carId
    await page.waitForURL(/\/cars\/(\d+)\/edit/);
    const carId = page.url().match(/\/cars\/(\d+)\/edit/)?.[1];

    await page.getByRole("button", { name: /submit for review/i }).click();

    // 提交后会真正跳转到列表页
    await page.waitForURL(/\/profile\/listings/);

    // 用 carId 精确定位到这张卡片，确认状态徽标显示的真实文字 "Pending"
    const listingCard = page.getByTestId(`listing-card-${carId}`);
    await expect(listingCard).toBeVisible({ timeout: 10000 });
    await expect(listingCard.getByText("Pending")).toBeVisible();
  });

  test("测试3：Admin 审核车辆通过", async ({ page, request }) => {
    const uid = Date.now().toString().slice(-6);
    const sellerEmail = `test-seller2-${uid}@example.com`;
    await createAndVerifyUser(request, sellerEmail);
    const sellerToken = await loginAndGetToken(request, sellerEmail);

    // 通过 API 创建并提交车辆（这不是这个测试的测试点，走 UI 浪费时间）
    const carRes = await request.post(`${API}/cars`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
      data: {
        title: `Admin Test Car ${uid}`,
        brand: "Honda",
        model: "Civic",
        year: 2019,
        price: 15000,
        mileage: 60000,
      },
    });
    const carId = (await carRes.json()).data.id;

    await request.post(`${API}/cars/${carId}/submit`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
    });

    // Admin 登录后跳转到 /admin，不是根路径，显式传第三个参数
    const loginPage = new LoginPage(page);
    await loginPage.loginAndWait(ADMIN_EMAIL, ADMIN_PASSWORD, "/admin");

    // 用 carId 拼出的 data-testid 精确定位到这张卡片，再在里面找 Approve 按钮
    const carCard = page.getByTestId(`pending-car-${carId}`);

    await expect(carCard).toBeVisible({ timeout: 10000 });

    await carCard.getByRole("button", { name: /approve/i }).click();

    // 审核通过后车辆从待审核列表消失
    await expect(carCard).not.toBeVisible({ timeout: 10000 });
  });

  // ── 测试4：买家下单 ────────────────────────────────────────
  // 车辆的准备（创建、提交、审核）全部走 API
  // 买家下单是这个测试真正要验证的行为，走 UI
  test("测试4：买家浏览车辆并下单", async ({ page, request }) => {
    const uid = Date.now().toString().slice(-6);

    // 准备一辆 Published 状态的车辆
    const sellerEmail = `test-seller3-${uid}@example.com`;
    await createAndVerifyUser(request, sellerEmail);
    const sellerToken = await loginAndGetToken(request, sellerEmail);

    const carRes = await request.post(`${API}/cars`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
      data: {
        title: `Published Car ${uid}`,
        brand: "Mazda",
        model: "CX-5",
        year: 2021,
        price: 30000,
        mileage: 20000,
      },
    });
    const carId = (await carRes.json()).data.id;

    await request.post(`${API}/cars/${carId}/submit`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
    });

    // Admin 审核通过（走 API，不走 UI）
    const adminToken = await loginAndGetToken(
      request,
      ADMIN_EMAIL,
      ADMIN_PASSWORD,
    );
    await request.post(`${API}/admin/cars/${carId}/approve`, {
      headers: { Authorization: `Bearer ${adminToken}` },
    });

    // 买家登录
    const buyerEmail = `test-buyer-${uid}@example.com`;
    await createAndVerifyUser(request, buyerEmail);
    const loginPage = new LoginPage(page);
    await loginPage.loginAndWait(buyerEmail, "Test@123456");

    // 进入车辆详情页
    await page.goto(`/cars/${carId}`);

    await page.getByRole("button", { name: /buy now/i }).click();

    // 确认下单对话框出现——用正文段落文字断言，避免跟 "Confirm Purchase" 按钮/标题重名冲突
    await expect(page.getByText(/you are about to purchase/i)).toBeVisible({
      timeout: 10000,
    });

    await page.getByRole("button", { name: /confirm purchase/i }).click();

    // 下单成功提示
    await expect(page.getByText(/order.*success/i)).toBeVisible({
      timeout: 10000,
    });
  });

  // ── 测试5：卖家查看收到的订单 ──────────────────────────────
  // 车辆和订单的准备全部走 API
  // 卖家查看订单页面是这个测试真正要验证的行为，走 UI
  test("测试5：卖家查看收到的订单", async ({ page, request }) => {
    const uid = Date.now().toString().slice(-6);

    // 准备车辆
    const sellerEmail = `test-seller4-${uid}@example.com`;
    await createAndVerifyUser(request, sellerEmail);
    const sellerToken = await loginAndGetToken(request, sellerEmail);

    const carRes = await request.post(`${API}/cars`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
      data: {
        title: `Seller Sales Car ${uid}`,
        brand: "Nissan",
        model: "Leaf",
        year: 2022,
        price: 25000,
        mileage: 10000,
      },
    });
    const carId = (await carRes.json()).data.id;

    await request.post(`${API}/cars/${carId}/submit`, {
      headers: { Authorization: `Bearer ${sellerToken}` },
    });

    const adminToken = await loginAndGetToken(
      request,
      ADMIN_EMAIL,
      ADMIN_PASSWORD,
    );
    await request.post(`${API}/admin/cars/${carId}/approve`, {
      headers: { Authorization: `Bearer ${adminToken}` },
    });

    // 买家下单
    const buyerEmail = `test-buyer2-${uid}@example.com`;
    await createAndVerifyUser(request, buyerEmail);
    const buyerToken = await loginAndGetToken(request, buyerEmail);
    await request.post(`${API}/orders`, {
      headers: { Authorization: `Bearer ${buyerToken}` },
      data: { carId },
    });

    // 卖家登录查看销售订单
    const loginPage = new LoginPage(page);
    await loginPage.loginAndWait(sellerEmail, "Test@123456");

    await page.goto("/profile/sales");

    await expect(page.getByText(`Seller Sales Car ${uid}`)).toBeVisible({
      timeout: 10000,
    });
  });
});
