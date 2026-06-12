// src/components/Layout.tsx
import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import {
  Menu,
  X,
  CarFront,
  User,
  LogOut,
  LayoutDashboard,
  ShoppingBag,
  Heart,
  PlusCircle,
  ListFilter,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuLabel,
} from "@/components/ui/dropdown-menu";
import { navLinkClass } from "@/lib/navLinkClass";
import { cn } from "@/lib/utils";
import { authApi } from "@/api";

export default function Layout() {
  const { user, isAuthenticated, clearAuth } = useAuthStore();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const isAdmin = user?.role === "Admin";

  const handleLogout = async () => {
    try {
      // 通知后端撤销 RefreshToken
      // 后端会把数据库里的 IsRevoked 设为 true，并清除 Cookie
      // 即使这个请求失败（网络问题），前端也要继续登出
      await authApi.logout();
    } catch {
      // 静默处理：不能因为后端失败导致用户无法登出
    } finally {
      // 清除前端状态（authStore + localStorage 里的 user）
      clearAuth();
      // 关闭移动端菜单
      setMobileOpen(false);
      // 跳转首页
      navigate("/");
    }
  };

  return (
    // ✅ 修复5：flex-col + min-h-screen 让 footer 沉底
    <div className="layout-root">
      {/* ── 导航栏 ── */}
      {/* ✅ 修复2：sticky + z-50 确保下拉菜单浮在页面内容之上 */}
      <header
        className="sticky top-0 z-50 border-b"
        style={{
          backgroundColor: "var(--color-surface)",
          borderColor: "var(--color-border-strong)",
          boxShadow: "var(--shadow-sm)",
        }}
      >
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6">
          {/* 左侧：Logo + 主导航 */}
          <div className="flex items-center gap-8">
            <NavLink to="/" className="flex items-center gap-2 group">
              <div
                className="flex h-8 w-8 items-center justify-center rounded-lg transition-opacity group-hover:opacity-90"
                style={{ backgroundColor: "var(--color-accent)" }}
              >
                <CarFront className="h-4 w-4 text-white" />
              </div>
              <span
                className="text-lg font-bold tracking-tight"
                style={{
                  color: "var(--color-accent)",
                  fontFamily: "'DM Serif Display', serif",
                }}
              >
                UUcars
              </span>
            </NavLink>

            {/* 桌面端导航 */}
            <nav className="hidden items-center gap-6 md:flex">
              <NavLink to="/" className={navLinkClass} end>
                Browse Cars
              </NavLink>
              {isAuthenticated() && !isAdmin && (
                <NavLink to="/profile/listings" className={navLinkClass}>
                  My Listings
                </NavLink>
              )}
              {isAdmin && (
                <NavLink to="/admin" className={navLinkClass}>
                  Admin Panel
                </NavLink>
              )}
            </nav>
          </div>

          {/* 右侧 */}
          <div className="flex items-center gap-3">
            {isAuthenticated() ? (
              <>
                {/* 普通用户：Sell a Car 按钮 */}
                {!isAdmin && (
                  <Button
                    variant="accent"
                    size="sm"
                    className="hidden sm:inline-flex gap-1.5"
                    onClick={() => navigate("/cars/new")}
                  >
                    <PlusCircle className="h-3.5 w-3.5" />
                    Sell a Car
                  </Button>
                )}

                {/* 用户下拉菜单 */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    {/* ✅ 修复4：明确 text-white */}
                    <button
                      className="flex h-9 w-9 items-center justify-center rounded-full text-sm font-semibold text-white transition-opacity hover:opacity-90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-primary)] focus-visible:ring-offset-2"
                      style={{ backgroundColor: "var(--color-primary)" }}
                      aria-label="User menu"
                    >
                      {user?.username?.charAt(0).toUpperCase()}
                    </button>
                  </DropdownMenuTrigger>

                  {/* ✅ 修复2：z-[100] 确保在 sticky header 之上 */}
                  <DropdownMenuContent
                    align="end"
                    className="z-[100] w-52 rounded-[var(--radius-lg)] border p-1.5"
                    style={{
                      borderColor: "var(--color-border)",
                      boxShadow: "var(--shadow-lg)",
                      backgroundColor: "var(--color-surface)",
                    }}
                  >
                    <DropdownMenuLabel className="px-2 py-1.5">
                      <p
                        className="text-sm font-semibold"
                        style={{ color: "var(--color-text-primary)" }}
                      >
                        {user?.username}
                      </p>
                      <p
                        className="truncate text-xs"
                        style={{ color: "var(--color-text-muted)" }}
                      >
                        {user?.email}
                      </p>
                    </DropdownMenuLabel>
                    <DropdownMenuSeparator
                      style={{ backgroundColor: "var(--color-border)" }}
                    />

                    {isAdmin ? (
                      // Admin 菜单
                      <DropdownMenuItem
                        onClick={() => navigate("/admin")}
                        className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2 py-1.5 text-sm"
                      >
                        <LayoutDashboard
                          className="h-4 w-4"
                          style={{ color: "var(--color-text-muted)" }}
                        />
                        Admin Panel
                      </DropdownMenuItem>
                    ) : (
                      // 普通用户菜单
                      <>
                        <DropdownMenuItem
                          onClick={() => navigate("/profile")}
                          className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2 py-1.5 text-sm"
                        >
                          <User
                            className="h-4 w-4"
                            style={{ color: "var(--color-text-muted)" }}
                          />
                          My Profile
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          onClick={() => navigate("/profile/listings")}
                          className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2 py-1.5 text-sm"
                        >
                          <ListFilter
                            className="h-4 w-4"
                            style={{ color: "var(--color-text-muted)" }}
                          />
                          My Listings
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          onClick={() => navigate("/profile/purchases")}
                          className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2 py-1.5 text-sm"
                        >
                          <ShoppingBag
                            className="h-4 w-4"
                            style={{ color: "var(--color-text-muted)" }}
                          />
                          My Purchases
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          onClick={() => navigate("/profile/favorites")}
                          className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2 py-1.5 text-sm"
                        >
                          <Heart
                            className="h-4 w-4"
                            style={{ color: "var(--color-text-muted)" }}
                          />
                          Saved Cars
                        </DropdownMenuItem>
                      </>
                    )}

                    <DropdownMenuSeparator
                      style={{ backgroundColor: "var(--color-border)" }}
                    />
                    <DropdownMenuItem
                      onClick={handleLogout}
                      className="flex cursor-pointer items-center gap-2 rounded-[var(--radius-sm)] px-2 py-1.5 text-sm"
                      style={{ color: "var(--color-danger)" }}
                    >
                      <LogOut className="h-4 w-4" />
                      Sign out
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </>
            ) : (
              <>
                <NavLink
                  to="/login"
                  className="text-sm font-medium"
                  style={{ color: "var(--color-text-secondary)" }}
                >
                  Sign in
                </NavLink>
                <Button asChild size="sm">
                  <NavLink to="/register" style={{ color: "white" }}>
                    Get started
                  </NavLink>
                </Button>
              </>
            )}

            {/* 移动端汉堡 */}
            <button
              className="flex h-9 w-9 items-center justify-center rounded-lg transition-colors md:hidden"
              style={{ color: "var(--color-text-secondary)" }}
              onClick={() => setMobileOpen(!mobileOpen)}
              aria-label="Toggle menu"
            >
              {mobileOpen ? (
                <X className="h-5 w-5" />
              ) : (
                <Menu className="h-5 w-5" />
              )}
            </button>
          </div>
        </div>

        {/* 移动端展开菜单 */}
        {mobileOpen && (
          <div
            className="border-t px-4 py-3 md:hidden"
            style={{
              backgroundColor: "var(--color-surface)",
              borderColor: "var(--color-border)",
            }}
          >
            <div className="space-y-1">
              <MobileNavLink to="/" onClick={() => setMobileOpen(false)} end>
                Browse Cars
              </MobileNavLink>
              {isAuthenticated() ? (
                <>
                  {!isAdmin && (
                    <>
                      <MobileNavLink
                        to="/cars/new"
                        onClick={() => setMobileOpen(false)}
                      >
                        Sell a Car
                      </MobileNavLink>
                      <MobileNavLink
                        to="/profile"
                        onClick={() => setMobileOpen(false)}
                      >
                        My Profile
                      </MobileNavLink>
                    </>
                  )}
                  {isAdmin && (
                    <MobileNavLink
                      to="/admin"
                      onClick={() => setMobileOpen(false)}
                    >
                      Admin Panel
                    </MobileNavLink>
                  )}
                  <button
                    onClick={handleLogout}
                    className="flex w-full items-center rounded-lg px-3 py-2.5 text-sm font-medium transition-colors"
                    style={{ color: "var(--color-danger)" }}
                  >
                    Sign out
                  </button>
                </>
              ) : (
                <>
                  <MobileNavLink
                    to="/login"
                    onClick={() => setMobileOpen(false)}
                  >
                    Sign in
                  </MobileNavLink>
                  <MobileNavLink
                    to="/register"
                    onClick={() => setMobileOpen(false)}
                  >
                    Get started
                  </MobileNavLink>
                </>
              )}
            </div>
          </div>
        )}
      </header>

      {/* ✅ 修复5：layout-main = flex-1，撑开高度 */}
      <main className="layout-main mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
        <Outlet />
      </main>

      {/* Footer 永远在底部 */}
      <footer
        className="border-t py-8"
        style={{
          borderColor: "var(--color-border)",
          backgroundColor: "var(--color-surface)",
        }}
      >
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-4 sm:flex-row sm:px-6">
          <div className="flex items-center gap-2">
            <CarFront
              className="h-4 w-4"
              style={{ color: "var(--color-accent)" }}
            />
            <span
              className="text-sm font-bold"
              style={{
                color: "var(--color-accent)",
                fontFamily: "'DM Serif Display', serif",
              }}
            >
              UUcars
            </span>
          </div>
          <p className="text-xs" style={{ color: "var(--color-text-muted)" }}>
            © {new Date().getFullYear()} UUcars · The trusted marketplace for
            Kiwi car buyers and sellers.
          </p>
        </div>
      </footer>
    </div>
  );
}

function MobileNavLink({
  to,
  onClick,
  children,
  end,
}: {
  to: string;
  onClick: () => void;
  children: React.ReactNode;
  end?: boolean;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      onClick={onClick}
      className={({ isActive }) =>
        cn(
          "flex items-center rounded-lg px-3 py-2.5 text-sm font-medium transition-colors",
          isActive
            ? "bg-[var(--color-primary-light)] text-[var(--color-primary)]"
            : "text-[var(--color-text-secondary)] hover:bg-[var(--color-bg)] hover:text-[var(--color-text-primary)]",
        )
      }
    >
      {children}
    </NavLink>
  );
}
