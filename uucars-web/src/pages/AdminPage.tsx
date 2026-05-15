import { NavLink, Outlet } from "react-router-dom";

const tabs = [{ to: "/admin/pending", label: "Pending Review" }];

export default function AdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Admin Panel</h1>
        <p className="text-gray-500">Manage car listings and users</p>
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

      <Outlet />
    </div>
  );
}
