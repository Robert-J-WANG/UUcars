import { Link, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "@/stores/authStore";
import { Button } from "@/components/ui/button";

export default function Layout() {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="border-b bg-white">
        <div
          className="mx-auto flex max-w-7xl items-center
                        justify-between px-4 py-4"
        >
          {/* Logo */}
          <Link to="/" className="text-xl font-bold text-blue-600">
            UUcars
          </Link>

          {/* 右侧导航 */}
          <div className="flex items-center gap-4">
            {isAuthenticated() ? (
              // 已登录：显示用户名和退出按钮
              <>
                <span className="text-sm text-gray-600">{user?.username}</span>
                <Button variant="outline" size="sm" onClick={handleLogout}>
                  Sign out
                </Button>
              </>
            ) : (
              // 未登录：显示登录和注册链接
              <>
                <Link
                  to="/login"
                  className="text-sm text-gray-600 hover:text-gray-900"
                >
                  Sign in
                </Link>
                <Button asChild size="sm">
                  <Link to="/register">Sign up</Link>
                </Button>
              </>
            )}
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}
