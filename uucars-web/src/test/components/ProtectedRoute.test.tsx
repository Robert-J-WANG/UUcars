import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import ProtectedRoute from "@/components/ProtectedRoute";

const mockIsAuthenticated = vi.fn();
vi.mock("@/stores/authStore", () => ({
  useAuthStore: vi.fn(() => ({
    isAuthenticated: mockIsAuthenticated,
  })),
}));

// 辅助：渲染一个包含 ProtectedRoute 的路由结构
// initialEntries 模拟初始 URL，ProtectedRoute 包裹一个假的受保护页面
const renderWithRoute = (initialPath: string) =>
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route element={<ProtectedRoute />}>
          <Route path="/protected" element={<div>Protected Content</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );

describe("ProtectedRoute", () => {
  it("未登录时应该重定向到登录页", () => {
    mockIsAuthenticated.mockReturnValue(false);

    renderWithRoute("/protected");

    expect(screen.getByText("Login Page")).toBeInTheDocument();
    expect(screen.queryByText("Protected Content")).not.toBeInTheDocument();
  });

  it("已登录时应该渲染受保护的页面内容", () => {
    mockIsAuthenticated.mockReturnValue(true);

    renderWithRoute("/protected");

    expect(screen.getByText("Protected Content")).toBeInTheDocument();
    expect(screen.queryByText("Login Page")).not.toBeInTheDocument();
  });
});
