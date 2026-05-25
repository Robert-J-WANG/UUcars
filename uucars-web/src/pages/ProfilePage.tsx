import { NavLink, Outlet } from "react-router-dom";
import { useAuthStore } from "@/stores/authStore";
import { ListFilter, Heart, ShoppingBag, TrendingUp } from "lucide-react";

const tabs = [
  { to: "/profile/listings", label: "My Listings", icon: ListFilter },
  { to: "/profile/favorites", label: "Saved Cars", icon: Heart },
  { to: "/profile/purchases", label: "My Purchases", icon: ShoppingBag },
  { to: "/profile/sales", label: "My Sales", icon: TrendingUp },
];

export default function ProfilePage() {
  const { user } = useAuthStore();

  return (
    <div className="space-y-6">
      {/* 页头 */}
      <div className="flex items-center gap-4">
        <div
          className="flex h-14 w-14 items-center justify-center rounded-2xl text-xl font-bold text-white"
          style={{ backgroundColor: "var(--color-primary)" }}
        >
          {user?.username?.charAt(0).toUpperCase()}
        </div>
        <div>
          <h1
            className="text-2xl"
            style={{
              color: "var(--color-text-primary)",
              fontFamily: "'DM Serif Display', serif",
            }}
          >
            {user?.username}
          </h1>
          <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
            {user?.email}
          </p>
        </div>
      </div>

      {/* 标签栏 */}
      <div className="border-b" style={{ borderColor: "var(--color-border)" }}>
        <nav className="flex gap-1">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <NavLink
                key={tab.to}
                to={tab.to}
                className={({ isActive }) =>
                  isActive
                    ? "flex items-center gap-1.5 border-b-2 px-4 py-2.5 text-sm font-semibold"
                    : "flex items-center gap-1.5 px-4 py-2.5 text-sm transition-colors"
                }
                style={({ isActive }) => ({
                  color: isActive
                    ? "var(--color-primary)"
                    : "var(--color-text-secondary)",
                  borderColor: isActive
                    ? "var(--color-primary)"
                    : "transparent",
                })}
              >
                <Icon className="h-3.5 w-3.5" />
                {tab.label}
              </NavLink>
            );
          })}
        </nav>
      </div>

      <Outlet />
    </div>
  );
}
