import { NavLink, Outlet } from "react-router-dom";
import { useAuthStore } from "@/stores/authStore";

const tabs = [
  { to: "/profile/listings", label: "My Listings" },
  { to: "/profile/favorites", label: "My Favorites" },
  { to: "/profile/purchases", label: "My Purchases" },
  { to: "/profile/sales", label: "My Sales" },
];

export default function ProfilePage() {
  const { user } = useAuthStore();

  return (
    <div className="space-y-6">
      {/* 页头 */}
      <div>
        <h1 className="text-2xl font-bold">My Profile</h1>
        <p className="text-gray-500">{user?.email}</p>
      </div>

      {/* 标签栏 */}
      <div className="border-b">
        <nav className="flex gap-1">
          {tabs.map((tab) => (
            <NavLink
              key={tab.to}
              to={tab.to}
              className={({ isActive }) =>
                isActive
                  ? "border-b-2 border-blue-600 px-4 py-2 text-sm font-semibold text-blue-600"
                  : "px-4 py-2 text-sm text-gray-600 hover:text-gray-900"
              }
            >
              {tab.label}
            </NavLink>
          ))}
        </nav>
      </div>

      {/* 子路由内容渲染在这里 */}
      {/* 切换标签时，只有这里的内容变化，上方的标签栏不重新渲染 */}
      <Outlet />
    </div>
  );
}
