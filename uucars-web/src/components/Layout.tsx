import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "@/stores/authStore";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "./ui/dropdown-menu";

export default function Layout() {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();

  // 判断是不是admin
  const isAdmin = user?.role === "Admin";

  /* ------------ 导航链接的样式函数 ----------- */
  // isActive 为 true 时：蓝色加粗
  // isActive 为 false 时：灰色，hover 时变深
  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    isActive
      ? "text-blue-600 font-semibold text-sm"
      : "text-gray-600 hover:text-gray-900 text-sm";

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-gray-100">
      <header className="border-b bg-white">
        <div
          className="mx-auto flex max-w-7xl items-center
                        justify-between px-4 py-4"
        >
          {/* 左侧*/}
          <div className="flex items-center gap-8">
            {/* Logo */}
            <NavLink to="/" className="text-xl font-bold text-blue-600">
              UUcars
            </NavLink>
            {/* 主导航 */}
            <nav className="flex items-center gap-6">
              <NavLink to="/" className={navLinkClass} end>
                Browse Cars
              </NavLink>
            </nav>
          </div>

          {/* 右侧 */}
          <div className="flex items-center gap-3">
            {isAuthenticated() ? (
              // 登录状态
              <>
                {/* 普通用户 发布车辆按钮  */}
                {!isAdmin && (
                  <Button asChild size="sm" variant="outline">
                    <NavLink to="/cars/new">Sell a Car</NavLink>
                  </Button>
                )}

                {/* 用户下拉菜单 */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="sm">
                      {user?.username}
                    </Button>
                  </DropdownMenuTrigger>

                  <DropdownMenuContent align="end" className="w-48">
                    {isAdmin ? (
                      // role="admin"
                      <>
                        <DropdownMenuItem onClick={() => navigate("/admin")}>
                          Admin Panel
                        </DropdownMenuItem>
                      </>
                    ) : (
                      // role="user"
                      <>
                        {/* 用户资料 */}
                        <DropdownMenuItem onClick={() => navigate("/profile")}>
                          My Profile
                        </DropdownMenuItem>

                        {/* 车辆发布列表 */}
                        <DropdownMenuItem
                          onClick={() => navigate("/profile/listings")}
                        >
                          My Listings
                        </DropdownMenuItem>

                        {/* 购买列表 */}
                        <DropdownMenuItem
                          onClick={() => navigate("/profile/purchases")}
                        >
                          My Purchases
                        </DropdownMenuItem>
                      </>
                    )}

                    {/* 分隔线 */}
                    <DropdownMenuSeparator />

                    {/* 退出登录 */}
                    <DropdownMenuItem
                      onClick={handleLogout}
                      className="text-red-600"
                    >
                      Sign out
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            ) : (
              // 未登录状态
              <>
                {/* 登录 */}
                <NavLink
                  to="/login"
                  className="text-sm text-gray-600 hover:text-gray-900"
                >
                  Sign in
                </NavLink>
                {/* 注册 */}
                <Button asChild size="sm">
                  <NavLink to="/register">Sign up</NavLink>
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
