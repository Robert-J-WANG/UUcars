import { authApi } from "@/api";
import GoogleSignInButton from "@/components/GoogleSignInButton";
import type { LoginResponse } from "@/types";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { toast } from "sonner";

// 测试时不发送真实 HTTP 请求。
// 把 authApi.googleLogin 替换成可以控制和检查的 Mock 函数。
vi.mock("@/api", () => ({
  authApi: {
    googleLogin: vi.fn(),
  },
}));

// 测试环境不能打开真实的 Google 登录窗口，
// 因此用一个普通按钮代替 GoogleLogin。
vi.mock("@react-oauth/google", () => ({
  GoogleLogin: ({
    onSuccess,
  }: {
    onSuccess: (response: { credential?: string }) => void;
  }) => (
    <button
      type="button"
      onClick={() =>
        // 点击按钮时，模拟 Google 登录成功，
        // 并返回一个 credential。
        onSuccess({
          credential: "google-id-token",
        })
      }
    >
      Mock Google sign in
    </button>
  ),
}));

// 模拟 UUcars 后端验证 Google ID Token 后返回的登录结果。
// 因为组件会等待 googleLogin 完成，所以需要准备这个返回值。
const loginResult: LoginResponse = {
  token: "uucars-access-token",
  expiresAt: "2026-09-03T12:00:00Z",
  user: {
    id: 2,
    username: "Alice",
    email: "alice@gmail.com",
    role: "User",
    createdAt: "2026-09-03T10:00:00Z",
  },
};

// 测试环境不需要真正显示 Sonner 提示框。
// 把 toast.error 替换成 Mock 函数，用来检查组件显示了什么错误。
vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
  },
}));

describe("GoogleSignInButton", () => {
  beforeEach(() => {
    // 清除上一个测试留下的 Mock 调用记录。
    vi.clearAllMocks();
  });

  /* -------------- 测试 1： ------------- */
  // 验证 Google 返回 credential 后，组件会把它传给 UUcars 的 Google 登录接口。
  it("应该把 Google credential 发送给后端", async () => {
    // 1. 规定 Mock 接口本次调用成功，并返回上面准备好的 UUcars 登录结果。
    vi.mocked(authApi.googleLogin).mockResolvedValueOnce(loginResult);

    // 2. 渲染要测试的组件。
    // 这个测试暂时不检查 onAuthenticated，所以传入一个空的 Mock 函数。
    render(<GoogleSignInButton onAuthenticated={vi.fn()} />);

    // 3. 用户点击模拟的 Google 登录按钮。
    // 创建用户
    const user = userEvent.setup();
    // 点击后，Mock GoogleLogin 会返回 "google-id-token"。
    await user.click(
      screen.getByRole("button", {
        name: /mock google sign in/i,
      }),
    );

    // 4. 检查组件是否把 Google 返回的 credential原样传给了 authApi.googleLogin
    await waitFor(() => {
      expect(authApi.googleLogin).toHaveBeenCalledWith("google-id-token");
    });
  });

  /* -------------- 测试 2： ------------- */
  // 验证后端登录成功后，组件会把返回的登录结果交给页面
  it("应该把 UUcars 登录结果交给页面", async () => {
    // 1. 规定 Mock 接口本次调用成功，并返回准备好的 UUcars 登录结果
    vi.mocked(authApi.googleLogin).mockResolvedValueOnce(loginResult);

    // 2. 创建一个 Mock 回调函数，记录 onAuthenticated 是否被调用以及收到的数据
    const mockOnAuthenticated = vi.fn();

    // 3. 渲染组件，并把 Mock 回调函数传给 onAuthenticated
    render(<GoogleSignInButton onAuthenticated={mockOnAuthenticated} />);

    // 4. 用户点击模拟的 Google 登录按钮。
    const user = userEvent.setup();

    await user.click(
      screen.getByRole("button", {
        name: /mock google sign in/i,
      }),
    );

    // 5. 检查组件是否把后端返回的 loginResult 交给了页面。
    await waitFor(() => {
      expect(mockOnAuthenticated).toHaveBeenCalledWith(loginResult);
    });
  });

  /* -------------- 测试 3： ------------- */
  // 验证后端拒绝 Google 登录时，组件会显示错误，
  // 并且不会把登录结果交给页面。
  it("后端拒绝 Google 登录时应该显示错误", async () => {
    // 1. 模拟后端验证失败。
    // mockRejectedValueOnce 表示这次调用不会返回 LoginResponse，
    // 而是返回一个被拒绝的 Promise，并抛出指定错误。
    vi.mocked(authApi.googleLogin).mockRejectedValueOnce(
      new Error("Invalid Google ID token"),
    );

    // 2. 创建 Mock 回调函数。
    // 如果登录失败，这个函数不应该被调用。
    const mockOnAuthenticated = vi.fn();

    // 3. 渲染组件，并把 Mock 回调传给 onAuthenticated。
    render(<GoogleSignInButton onAuthenticated={mockOnAuthenticated} />);

    // 4. 用户点击模拟的 Google 登录按钮。
    // Mock GoogleLogin 会把 "google-id-token" 交给组件。
    const user = userEvent.setup();

    await user.click(
      screen.getByRole("button", {
        name: /mock google sign in/i,
      }),
    );

    // 5. googleLogin 是异步调用。
    // 等待组件捕获错误后，检查 toast.error 收到的错误信息。
    await waitFor(() => {
      expect(toast.error).toHaveBeenCalledWith("Invalid Google ID token");
    });

    // 6. 后端没有返回登录结果，
    // 因此组件不能调用登录成功回调。
    expect(mockOnAuthenticated).not.toHaveBeenCalled();
  });
});
