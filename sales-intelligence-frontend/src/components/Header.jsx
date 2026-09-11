import { useState, useRef, useEffect } from "react";
import { ChevronDown, LogOut, Upload, Database, Layers, Trash2 } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useDataset } from "../context/DatasetContext";
import { useNavigate } from "react-router-dom";
import UploadModal from "./UploadModal";

export default function Header() {
  const { user, logout } = useAuth();
  const { datasets, selectedDatasetId, setSelectedDatasetId, setIsUploadModalOpen, deleteDataset } = useDataset();
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const dropdownRef = useRef(null);
  const navigate = useNavigate();

  const userName = user?.fullName || user?.name || "Admin User";
  const userEmail = user?.email || "admin@salesintelligence.com";
  const userRole = user?.role || "Administrator";
  const avatarLetter = userName.charAt(0).toUpperCase();

  const selectedDatasetObj = datasets.find((d) => Number(d.id) === Number(selectedDatasetId));

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

  const handleDeleteActiveDataset = async () => {
    if (!selectedDatasetId || selectedDatasetId === 0) return;
    const name = selectedDatasetObj?.name || `Dataset #${selectedDatasetId}`;
    const confirmed = window.confirm(
      `Are you sure you want to delete dataset "${name}"?\n\nThis will permanently remove all associated deals, accounts, and agents from the database and dataset list.`
    );
    if (!confirmed) return;

    setDeleting(true);
    try {
      await deleteDataset(selectedDatasetId);
    } catch (err) {
      alert(err.response?.data?.message || "Failed to delete dataset.");
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <header className="dashboard-header" style={{ justifyContent: "space-between", alignItems: "center" }}>
        {/* LEFT SIDE: DATASET SELECTOR DROPDOWN & UPLOAD BUTTON */}
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              background: "#1e293b",
              border: "1px solid #334155",
              borderRadius: "8px",
              padding: "6px 12px",
              color: "#f8fafc",
            }}
          >
            <Database size={16} color="#818cf8" />
            <span style={{ fontSize: "0.85rem", color: "#94a3b8", fontWeight: 500 }}>Active Dataset:</span>
            <select
              value={selectedDatasetId}
              onChange={(e) => setSelectedDatasetId(Number(e.target.value))}
              style={{
                background: "transparent",
                border: "none",
                color: "#f8fafc",
                fontWeight: 600,
                fontSize: "0.875rem",
                cursor: "pointer",
                outline: "none",
              }}
            >
              <option value={0} style={{ background: "#1e293b", color: "#fff" }}>
                All Datasets (Combined)
              </option>
              {datasets.map((d) => (
                <option key={d.id} value={d.id} style={{ background: "#1e293b", color: "#fff" }}>
                  {d.name} ({d.dealsCount || d.rowCount} deals)
                </option>
              ))}
            </select>

            {selectedDatasetId > 0 && (
              <button
                type="button"
                onClick={handleDeleteActiveDataset}
                disabled={deleting}
                title="Delete active dataset"
                style={{
                  background: "transparent",
                  border: "none",
                  color: "#f87171",
                  cursor: "pointer",
                  padding: "2px 4px",
                  display: "flex",
                  alignItems: "center",
                  borderRadius: "4px",
                  transition: "background 0.2s",
                }}
                onMouseEnter={(e) => (e.currentTarget.style.background = "#334155")}
                onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>

          <button
            type="button"
            onClick={() => setIsUploadModalOpen(true)}
            style={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              background: "linear-gradient(135deg, #6366f1 0%, #4f46e5 100%)",
              border: "none",
              borderRadius: "8px",
              padding: "7px 14px",
              color: "#ffffff",
              fontSize: "0.85rem",
              fontWeight: 600,
              cursor: "pointer",
              boxShadow: "0 4px 12px rgba(79, 70, 229, 0.25)",
              transition: "transform 0.15s, boxShadow 0.15s",
            }}
            onMouseEnter={(e) => (e.currentTarget.style.transform = "translateY(-1px)")}
            onMouseLeave={(e) => (e.currentTarget.style.transform = "translateY(0)")}
          >
            <Upload size={15} />
            + Upload New Dataset
          </button>
        </div>

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

      <UploadModal />
    </>
  );
}