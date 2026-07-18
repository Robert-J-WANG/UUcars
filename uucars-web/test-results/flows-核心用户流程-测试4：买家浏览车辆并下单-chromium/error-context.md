# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: flows.spec.ts >> 核心用户流程 >> 测试4：买家浏览车辆并下单
- Location: e2e/flows.spec.ts:105:3

# Error details

```
TypeError: Cannot read properties of null (reading 'token')
```

# Test source

```ts
  1  | import { type APIRequestContext } from "@playwright/test";
  2  | 
  3  | const BASE_URL = "http://localhost:5065";
  4  | 
  5  | // 注册用户并验证邮箱
  6  | // 为什么通过 API 而不是走 UI？
  7  | // 节省时间：E2E 测试本身已经够慢了，准备数据尽量走 API
  8  | // UI 注册流程在测试1里专门测，其他测试不需要重复这个步骤
  9  | export async function createAndVerifyUser(
  10 |   request: APIRequestContext,
  11 |   email: string,
  12 |   password = "Test@123456",
  13 | ) {
  14 |   // 从邮箱前缀截取 username，截短到合理长度避免超过字段长度限制
  15 |   const username = email.split("@")[0].slice(0, 20);
  16 | 
  17 |   // 1. 注册
  18 |   await request.post(`${BASE_URL}/auth/register`, {
  19 |     data: { email, username, password },
  20 |   });
  21 | 
  22 |   // 2. 取出验证 Token（走测试辅助接口，不走邮件）
  23 |   const tokenRes = await request.get(
  24 |     `${BASE_URL}/auth/test-verification-token?email=${encodeURIComponent(email)}`,
  25 |   );
  26 |   const { token } = await tokenRes.json();
  27 | 
  28 |   // 3. 验证邮箱
  29 |   await request.get(`${BASE_URL}/auth/verify-email?token=${token}`);
  30 | 
  31 |   return { email, password, username };
  32 | }
  33 | 
  34 | // 登录并拿到 JWT Token（供需要认证的 API 调用使用）
  35 | export async function loginAndGetToken(
  36 |   request: APIRequestContext,
  37 |   email: string,
  38 |   password = "Test@123456",
  39 | ): Promise<string> {
  40 |   const res = await request.post(`${BASE_URL}/auth/login`, {
  41 |     data: { email, password },
  42 |   });
  43 |   const body = await res.json();
> 44 |   return body.data.token;
     |                    ^ TypeError: Cannot read properties of null (reading 'token')
  45 | }
  46 | 
```