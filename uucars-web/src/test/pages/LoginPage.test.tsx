import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import LoginPage from "@/pages/LoginPage";
import type { LoginResponse } from "@/types";

// Mock 路由 hook（LoginPage 里用了 useNavigate 和 useLocation）
// vi.importActual 保留 react-router-dom 里其他真实导出（比如 MemoryRouter 本身）
// 只替换 useNavigate/useLocation 这两个 hook
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ state: null, pathname: "/login" }),
  };
});

// Mock authApi：避免真实发 HTTP 请求
vi.mock("@/api", () => ({
  authApi: {
    login: vi.fn(),
  },
}));

// Mock authStore：避免真实写 localStorage
const mockSetAuth = vi.fn();
vi.mock("@/stores/authStore", () => ({
  useAuthStore: vi.fn(() => ({
    setAuth: mockSetAuth,
  })),
}));

// 当前测试只关心 LoginPage 收到登录结果后如何处理。
// 因此用普通按钮代替真实的 GoogleSignInButton。
vi.mock("@/components/GoogleSignInButton", () => ({
  default: ({
    onAuthenticated,
  }: {
    onAuthenticated: (result: LoginResponse) => void;
  }) => (
    <button
      type="button"
      onClick={() =>
        // 点击按钮时，模拟 GoogleSignInButton
        // 已经取得后端返回的 UUcars 登录结果。
        onAuthenticated({
          token: "google-uucars-token",
          expiresAt: "2026-09-03T12:00:00Z",
          user: {
            id: 2,
            username: "Google User",
            email: "google-user@gmail.com",
            role: "User",
            createdAt: "2026-09-03T10:00:00Z",
          },
        })
      }
    >
      Mock Google sign in
    </button>
  ),
}));

const renderLoginPage = () =>
  render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>,
  );

describe("LoginPage", () => {
  beforeEach(() => {
    // 每个测试前清除所有 mock 的调用记录
    // 避免上一个测试用例的调用历史影响下一个（比如上一个测试
    // 断言过 login 被调用一次，这个记录不清除会累加到下一个测试里）
    vi.clearAllMocks();
  });

  it("空表单提交时应该显示验证错误", async () => {
    renderLoginPage();
    const user = userEvent.setup();

    // 直接点提交，不填任何内容
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    // RHF + Zod 验证失败，应该显示错误提示
    // 这里必须用 waitFor：Zod resolver 内部走的是 Promise 链，
    // 即使校验规则本身是同步判断（比如 min(1)），从触发校验到
    // 错误信息真正写回 DOM 之间仍然隔着至少一个微任务队列的延迟，
    // 直接同步断言会因为 DOM 还没更新而失败
    await waitFor(() => {
      expect(screen.getByText(/password is required/i)).toBeInTheDocument();
    });
  });

  it("输入无效邮箱格式时应该显示格式错误", async () => {
    renderLoginPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), "not-an-email");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid email/i)).toBeInTheDocument();
    });
  });

  it("填写正确后点击提交应该调用 authApi.login", async () => {
    // 让 Mock 的 login 函数这一次返回一个成功结果
    const { authApi } = await import("@/api");
    vi.mocked(authApi.login).mockResolvedValueOnce({
      token: "fake-token",
      user: {
        id: 1,
        username: "testuser",
        email: "test@example.com",
        role: "User",
      },
    } as never);

    renderLoginPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      // 验证 login 确实被调用，且传了正确的参数
      expect(authApi.login).toHaveBeenCalledWith({
        email: "test@example.com",
        password: "password123",
      });
    });
  });

  it("API 返回错误时应该显示服务端错误信息", async () => {
    const { authApi } = await import("@/api");
    // 让 Mock 的 login 函数这一次抛出一个错误，模拟服务端返回失败
    vi.mocked(authApi.login).mockRejectedValueOnce(
      new Error("Invalid email or password"),
    );

    renderLoginPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "wrongpassword");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/invalid email or password/i),
      ).toBeInTheDocument();
    });
  });

  /* -------------- 测试 4： ------------- */
  // 验证 LoginPage 收到 Google 登录结果后，
  // 会保存用户和 Access Token，并跳转到首页。
  it("Google 登录成功后应该保存状态并跳转", async () => {
    // 1. 渲染 LoginPage。
    // 页面中的 GoogleSignInButton 已经被替换成普通测试按钮。
    renderLoginPage();

    // 2. 用户点击模拟的 Google 登录按钮。
    const user = userEvent.setup();

    await user.click(
      screen.getByRole("button", {
        name: /mock google sign in/i,
      }),
    );

    // 3. 检查 LoginPage 是否把后端返回的
    // User 和 Access Token 保存到认证状态中。
    expect(mockSetAuth).toHaveBeenCalledWith(
      {
        id: 2,
        username: "Google User",
        email: "google-user@gmail.com",
        role: "User",
        createdAt: "2026-09-03T10:00:00Z",
      },
      "google-uucars-token",
    );

    // 4. 当前没有来源页面，并且登录用户不是 Admin，
    // 所以登录成功后应该进入首页。
    expect(mockNavigate).toHaveBeenCalledWith("/", {
      replace: true,
    });
  });
});
