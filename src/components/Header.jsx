import {
  Bell,
  Search,
  ChevronDown,
} from "lucide-react";

export default function Header() {
  return (
    <header className="dashboard-header">

      {/* SEARCH */}
      <div className="header-search">

        <Search size={18} />

        <input
          type="text"
          placeholder="Search..."
        />

      </div>


      {/* RIGHT SIDE */}
      <div className="header-right">

        {/* NOTIFICATION */}
        <button
          type="button"
          className="notification-button"
        >
          <Bell size={19} />

          <span className="notification-dot" />
        </button>


        {/* PROFILE */}
        <div className="header-profile">

          <div className="header-avatar">
            A
          </div>

          <div className="header-user-info">

            <strong>Admin User</strong>

            <span>Administrator</span>

          </div>

          <ChevronDown size={16} />

        </div>

      </div>

    </header>
  );
}