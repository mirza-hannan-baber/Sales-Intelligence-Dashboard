import {
  LayoutDashboard,
  TrendingUp,
  Users,
  UserRound,
  Handshake,
  Target,
  UserCog,
  LogOut,
  BarChart3,
  ShieldCheck,
  UserCheck
} from "lucide-react";

import { NavLink } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function Sidebar() {
  const { user, logout, isSuperadmin } = useAuth();

  const menuItems = [
    {
      section: "MAIN",
      items: [
        {
          name: "Dashboard",
          path: "/dashboard",
          icon: LayoutDashboard,
        },
      ],
    },
    {
      section: "ANALYTICS & PREDICTIONS",
      items: [
        {
          name: "Revenue Forecast",
          path: "/revenue-forecast",
          icon: TrendingUp,
        },
        {
          name: "Employee Revenue",
          path: "/employee-revenue",
          icon: BarChart3,
        },
        {
          name: "Employee Performance",
          path: "/employee-performance",
          icon: Users,
        },
        {
          name: "Deal Probability",
          path: "/deal-probability",
          icon: Target,
        },
      ],
    },
    {
      section: "MANAGEMENT",
      items: [
        {
          name: "Deals",
          path: "/deals",
          icon: Handshake,
        },
        {
          name: "Employees",
          path: "/employees",
          icon: UserRound,
        },
        ...(isSuperadmin
          ? [
              {
                name: "User Management",
                path: "/users",
                icon: UserCog,
              },
            ]
          : []),
      ],
    },
  ];

  return (
    <aside className="sidebar">

      {/* LOGO */}
      <div className="sidebar-logo">
        <div className="sidebar-logo-icon">SI</div>
        <div>
          <h2>Sales Intelligence</h2>
          <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            {isSuperadmin ? <ShieldCheck size={12} color="#6366f1" /> : <UserCheck size={12} color="#10b981" />}
            {user?.role || "Portal"}
          </span>
        </div>
      </div>

      {/* NAVIGATION */}
      <nav className="sidebar-navigation">
        {menuItems.map((section) => (
          <div className="sidebar-section" key={section.section}>
            <p className="sidebar-section-title">{section.section}</p>
            {section.items.map((item) => {
              const Icon = item.icon;
              return (
                <NavLink
                  key={item.path}
                  to={item.path}
                  className={({ isActive }) =>
                    `sidebar-link ${isActive ? "active" : ""}`
                  }
                >
                  <Icon size={18} />
                  <span>{item.name}</span>
                </NavLink>
              );
            })}
          </div>
        ))}
      </nav>

      {/* BOTTOM LOGOUT & USER INFO */}
      <div className="sidebar-bottom">
        <div style={{ padding: '0 12px 12px 12px', fontSize: '0.85rem', color: '#94a3b8' }}>
          <div style={{ fontWeight: '600', color: '#f8fafc' }}>{user?.fullName || "User"}</div>
          <div style={{ fontSize: '0.75rem' }}>{user?.email}</div>
        </div>
        <button
          type="button"
          className="sidebar-link logout-button"
          onClick={logout}
        >
          <LogOut size={18} />
          <span>Logout</span>
        </button>
      </div>

    </aside>
  );
}