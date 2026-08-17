import { useState, useRef, useEffect } from "react";
import { ChevronDown, LogOut, User as UserIcon } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useNavigate } from "react-router-dom";

export default function Header() {
  const { user, logout } = useAuth();
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  const userName = user?.fullName || user?.name || "Admin User";
  const userEmail = user?.email || "admin@salesintelligence.com";
  const userRole = user?.role || "Administrator";
  const avatarLetter = userName.charAt(0).toUpperCase();

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setDropdownOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleLogout = () => {
    setDropdownOpen(false);
    logout();
  };

  const handleProfileClick = () => {
    setDropdownOpen(false);
    navigate("/profile");
  };

  return (
    <header className="dashboard-header" style={{ justifyContent: "flex-end" }}>
      {/* RIGHT SIDE */}
      <div className="header-right" ref={dropdownRef} style={{ position: "relative" }}>
        {/* PROFILE */}
        <div
          className="header-profile"
          onClick={() => setDropdownOpen((prev) => !prev)}
          style={{ cursor: "pointer", userSelect: "none" }}
        >
          <div className="header-avatar">{avatarLetter}</div>
          <div className="header-user-info">
            <strong>{userName}</strong>
            <span>{userRole}</span>
          </div>
          <ChevronDown size={16} style={{ transform: dropdownOpen ? "rotate(180deg)" : "none", transition: "transform 0.2s" }} />
        </div>

        {/* USER DROPDOWN MENU */}
        {dropdownOpen && (
          <div
            style={{
              position: "absolute",
              top: "calc(100% + 8px)",
              right: 0,
              width: "240px",
              background: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              boxShadow: "0 10px 25px -5px rgba(0,0,0,0.4)",
              zIndex: 1000,
              overflow: "hidden",
              color: "#f8fafc",
            }}
          >
            <div style={{ padding: "12px 16px", borderBottom: "1px solid #334155" }}>
              <div style={{ fontWeight: 600, fontSize: "0.9rem", color: "#f8fafc" }}>{userName}</div>
              <div style={{ fontSize: "0.8rem", color: "#94a3b8", wordBreak: "break-all" }}>{userEmail}</div>
            </div>

            <div style={{ padding: "4px 0" }}>
              

              <button
                type="button"
                onClick={handleLogout}
                style={{
                  width: "100%",
                  padding: "10px 16px",
                  display: "flex",
                  alignItems: "center",
                  gap: "10px",
                  background: "transparent",
                  border: "none",
                  color: "#f87171",
                  fontSize: "0.875rem",
                  cursor: "pointer",
                  textAlign: "left",
                }}
                onMouseEnter={(e) => (e.currentTarget.style.background = "#334155")}
                onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
              >
                <LogOut size={16} />
                Logout
              </button>
            </div>
          </div>
        )}
      </div>
    </header>
  );
}