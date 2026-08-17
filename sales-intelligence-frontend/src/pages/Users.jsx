import { useState, useEffect, useMemo } from "react";
import {
  Search,
  Plus,
  Users as UsersIcon,
  UserCheck,
  ShieldCheck,
  UserX,
  Loader2,
  UserCog,
  CheckCircle,
  XCircle,
} from "lucide-react";
import { userService } from "../services/api";

export default function Users() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState("All");

  // Modal State
  const [showModal, setShowModal] = useState(false);
  const [newUser, setNewUser] = useState({
    fullName: "",
    email: "",
    password: "User123!",
    role: "User",
    department: "Sales",
  });

  useEffect(() => {
    fetchUsers();
  }, [search, roleFilter]);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const data = await userService.getUsers({ search, role: roleFilter });
      setUsers(data || []);
    } catch (err) {
      console.error("Error fetching users:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateUser = async (e) => {
    e.preventDefault();
    try {
      await userService.createUser(newUser);
      setShowModal(false);
      setNewUser({ fullName: "", email: "", password: "User123!", role: "User", department: "Sales" });
      fetchUsers();
    } catch (err) {
      console.error("Error creating user:", err);
    }
  };

  const handleToggleRole = async (user) => {
    const targetRole = user.role === "Superadmin" ? "User" : "Superadmin";
    try {
      await userService.updateUser(user.id, {
        fullName: user.name,
        role: targetRole,
        department: user.department,
        isActive: user.status === "Active",
      });
      fetchUsers();
    } catch (err) {
      console.error("Error updating role:", err);
    }
  };

  const handleToggleActive = async (user) => {
    const targetStatus = user.status !== "Active";
    try {
      await userService.updateUser(user.id, {
        fullName: user.name,
        role: user.role,
        department: user.department,
        isActive: targetStatus,
      });
      fetchUsers();
    } catch (err) {
      console.error("Error toggling user status:", err);
    }
  };

  const stats = [
    {
      title: "Total Users",
      value: users.length.toString(),
      change: "Registered Accounts",
      icon: UsersIcon,
    },
    {
      title: "Active Users",
      value: users.filter((u) => u.status === "Active").length.toString(),
      change: "Active Status",
      icon: UserCheck,
    },
    {
      title: "Superadmins",
      value: users.filter((u) => u.role === "Superadmin").length.toString(),
      change: "Full Access",
      icon: ShieldCheck,
    },
    {
      title: "Standard Users",
      value: users.filter((u) => u.role === "User").length.toString(),
      change: "Dashboard Access",
      icon: UserX,
    },
  ];

  return (
    <div className="users-page">
      {/* PAGE HEADER */}
      <div className="page-title">
        <div>
          <h1>User Management</h1>
          <p>Manage system users, 2-role permissions (Superadmin / User) & access control</p>
        </div>
        <button className="primary-button" onClick={() => setShowModal(true)}>
          <Plus size={17} />
          Add User
        </button>
      </div>

      {/* STATS */}
      <div className="stats-grid">
        {stats.map((stat) => {
          const Icon = stat.icon;
          return (
            <div className="stat-card" key={stat.title}>
              <div className="stat-card-top">
                <div className="stat-icon">
                  <Icon size={19} />
                </div>
                <span className="stat-change">{stat.change}</span>
              </div>
              <p>{stat.title}</p>
              <h2>{stat.value}</h2>
            </div>
          );
        })}
      </div>

      {/* USERS TABLE */}
      <div className="users-table-card">
        <div className="users-toolbar">
          <div className="users-search">
            <Search size={17} />
            <input
              type="text"
              placeholder="Search users by name or email..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <select
            className="users-filter"
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
          >
            <option value="All">All Roles</option>
            <option value="Superadmin">Superadmin</option>
            <option value="User">User</option>
          </select>
        </div>

        <div className="users-table-wrapper">
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", padding: "40px", color: "#6366f1" }}>
              <Loader2 className="animate-spin" size={24} />
            </div>
          ) : (
            <table className="users-table">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Role</th>
                  <th>Department</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <div className="user-info">
                        <div className="user-avatar">
                          {user.name.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <strong>{user.name}</strong>
                          <span>{user.email}</span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className="user-role" style={{ background: user.role === "Superadmin" ? "#e0e7ff" : "#f1f5f9", color: user.role === "Superadmin" ? "#3730a3" : "#334155", padding: "4px 8px", borderRadius: "6px", fontWeight: "600", fontSize: "0.8rem" }}>
                        {user.role}
                      </span>
                    </td>
                    <td>{user.department}</td>
                    <td>
                      <span className={`user-status ${user.status === "Active" ? "user-active" : "user-inactive"}`}>
                        {user.status}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: "8px" }}>
                        <button
                          onClick={() => handleToggleRole(user)}
                          title="Promote/Demote Role"
                          style={{ padding: "4px 8px", fontSize: "0.75rem", background: "#ede9fe", color: "#5b21b6", border: "1px solid #ddd6fe", borderRadius: "4px", cursor: "pointer" }}
                        >
                          Set to {user.role === "Superadmin" ? "User" : "Superadmin"}
                        </button>
                        <button
                          onClick={() => handleToggleActive(user)}
                          title="Toggle Active/Inactive Status"
                          style={{ padding: "4px 8px", fontSize: "0.75rem", background: user.status === "Active" ? "#fef2f2" : "#f0fdf4", color: user.status === "Active" ? "#991b1b" : "#166534", border: `1px solid ${user.status === "Active" ? "#fca5a5" : "#86efac"}`, borderRadius: "4px", cursor: "pointer" }}
                        >
                          {user.status === "Active" ? "Deactivate" : "Activate"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {users.length === 0 && (
                  <tr>
                    <td colSpan="5" className="users-empty">
                      No users found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* CREATE USER MODAL */}
      {showModal && (
        <div style={{ position: "fixed", inset: 0, background: "rgba(15, 23, 42, 0.75)", display: "flex", justifyContent: "center", alignItems: "center", zIndex: 1000 }}>
          <div style={{ background: "#ffffff", padding: "24px", borderRadius: "12px", width: "100%", maxWidth: "450px" }}>
            <h2 style={{ marginBottom: "16px" }}>Create System User</h2>
            <form onSubmit={handleCreateUser}>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Full Name</label>
                <input type="text" required value={newUser.fullName} onChange={(e) => setNewUser({ ...newUser, fullName: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Email Address</label>
                <input type="email" required value={newUser.email} onChange={(e) => setNewUser({ ...newUser, email: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Password</label>
                <input type="password" required value={newUser.password} onChange={(e) => setNewUser({ ...newUser, password: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }} />
              </div>
              <div style={{ marginBottom: "12px" }}>
                <label style={{ display: "block", fontSize: "0.85rem", fontWeight: "600", marginBottom: "4px" }}>Role</label>
                <select value={newUser.role} onChange={(e) => setNewUser({ ...newUser, role: e.target.value })} style={{ width: "100%", padding: "8px", borderRadius: "6px", border: "1px solid #cbd5e1" }}>
                  <option value="User">User (Standard Dashboard User)</option>
                  <option value="Superadmin">Superadmin (Full Control)</option>
                </select>
              </div>
              <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px", marginTop: "20px" }}>
                <button type="button" onClick={() => setShowModal(false)} className="secondary-button" style={{ padding: "8px 16px" }}>Cancel</button>
                <button type="submit" className="primary-button" style={{ padding: "8px 16px" }}>Create User</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}