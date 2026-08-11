import {
  LayoutDashboard,
  TrendingUp,
  Users,
  UserRound,
  Handshake,
  Target,
  UserCog,
  Settings,
  UserCircle,
  LogOut,
  BarChart3,
} from "lucide-react";

import { NavLink } from "react-router-dom";

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
    section: "ANALYTICS",
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
      {
        name: "User Management",
        path: "/users",
        icon: UserCog,
      },
    ],
  },
];

export default function Sidebar() {
  return (
    <aside className="sidebar">

      {/* LOGO */}
      <div className="sidebar-logo">

        <div className="sidebar-logo-icon">
          SI
        </div>

        <div>
          <h2>Sales Intelligence</h2>
          <span>Admin Portal</span>
        </div>

      </div>


      {/* NAVIGATION */}
      <nav className="sidebar-navigation">

        {menuItems.map((section) => (
          <div
            className="sidebar-section"
            key={section.section}
          >

            <p className="sidebar-section-title">
              {section.section}
            </p>

            {section.items.map((item) => {

              const Icon = item.icon;

              return (
                <NavLink
                  key={item.path}
                  to={item.path}
                  className={({ isActive }) =>
                    `sidebar-link ${
                      isActive ? "active" : ""
                    }`
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


      {/* BOTTOM */}
      <div className="sidebar-bottom">

        <NavLink
          to="/profile"
          className="sidebar-link"
        >
          <UserCircle size={18} />
          <span>Profile</span>
        </NavLink>

        <NavLink
          to="/settings"
          className="sidebar-link"
        >
          <Settings size={18} />
          <span>Settings</span>
        </NavLink>

        <button
          type="button"
          className="sidebar-link logout-button"
        >
          <LogOut size={18} />
          <span>Logout</span>
        </button>

      </div>

    </aside>
  );
}