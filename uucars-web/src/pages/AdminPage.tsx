// src/pages/AdminPage.tsx
import { NavLink, Outlet } from "react-router-dom";
import { ClipboardList } from "lucide-react";

const tabs = [
  { to: "/admin/pending", label: "Pending Review", icon: ClipboardList },
];

export default function AdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1
          className="text-2xl"
          style={{
            color: "var(--color-text-primary)",
            fontFamily: "'DM Serif Display', serif",
          }}
        >
          Admin Panel
        </h1>
        <p className="text-sm" style={{ color: "var(--color-text-muted)" }}>
          Review and manage car listings
        </p>
      </div>

      {/* Tab 栏 */}
      <div className="border-b" style={{ borderColor: "var(--color-border)" }}>
        <nav className="-mb-px flex gap-0">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <NavLink
                key={tab.to}
                to={tab.to}
                // ✅ 关键修复：用 style prop 而非 className 字符串拼接处理 border-bottom
                // Tailwind 的 border-b-2 在 NavLink 动态 className 里会有优先级问题
                className="flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium
                           transition-colors duration-150"
                style={({ isActive }) => ({
                  color: isActive
                    ? "var(--color-primary)"
                    : "var(--color-text-secondary)",
                  borderBottom: isActive
                    ? "2px solid var(--color-primary)"
                    : "2px solid transparent",
                  marginBottom: "-1px", // 覆盖父级的 border-b，视觉上和 tab 栏无缝衔接
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
