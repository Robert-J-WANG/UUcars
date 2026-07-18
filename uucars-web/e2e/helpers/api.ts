import { type APIRequestContext } from "@playwright/test";

const BASE_URL = "http://localhost:5065";

// 注册用户并验证邮箱
// 为什么通过 API 而不是走 UI？
// 节省时间：E2E 测试本身已经够慢了，准备数据尽量走 API
// UI 注册流程在测试1里专门测，其他测试不需要重复这个步骤
export async function createAndVerifyUser(
  request: APIRequestContext,
  email: string,
  password = "Test@123456",
) {
  // 从邮箱前缀截取 username，截短到合理长度避免超过字段长度限制
  const username = email.split("@")[0].slice(0, 20);

  // 1. 注册
  await request.post(`${BASE_URL}/auth/register`, {
    data: { email, username, password },
  });

  // 2. 取出验证 Token（走测试辅助接口，不走邮件）
  const tokenRes = await request.get(
    `${BASE_URL}/auth/test-verification-token?email=${encodeURIComponent(email)}`,
  );
  const { token } = await tokenRes.json();

  // 3. 验证邮箱
  await request.get(`${BASE_URL}/auth/verify-email?token=${token}`);

  return { email, password, username };
}

// 登录并拿到 JWT Token（供需要认证的 API 调用使用）
export async function loginAndGetToken(
  request: APIRequestContext,
  email: string,
  password = "Test@123456",
): Promise<string> {
  const res = await request.post(`${BASE_URL}/auth/login`, {
    data: { email, password },
  });
  const body = await res.json();
  return body.data.token;
}
