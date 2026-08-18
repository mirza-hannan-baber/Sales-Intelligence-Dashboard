import { useState, useEffect } from "react";
import {
  Search,
  Users,
  UserCheck,
  UserX,
  TrendingUp,
  Loader2,
  Building2,
} from "lucide-react";
import { agentsService } from "../services/api";

export default function Employees() {
  const [agents, setAgents] = useState([]);
  const [statsData, setStatsData] = useState({
    totalAgents: 0,
    avgRevenue: 0,
    avgWinRate: 0,
  });
  const [regionalOffices, setRegionalOffices] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [regionalOffice, setRegionalOffice] = useState("All");

  useEffect(() => {
    fetchAgents();
  }, [search, regionalOffice]);

  const fetchAgents = async () => {
    setLoading(true);
    try {
      const data = await agentsService.getAgents({ search, regionalOffice });
      setAgents(data.items || []);
      setStatsData({
        totalAgents: data.totalAgents || 0,
        avgRevenue: data.avgRevenue || 0,
        avgWinRate: data.avgWinRate || 0,
      });
      if (data.filterOptions && data.filterOptions.regionalOffices) {
        setRegionalOffices(data.filterOptions.regionalOffices);
      }
    } catch (err) {
      console.error("Error fetching agents:", err);
    } finally {
      setLoading(false);
    }
  };

  const statCards = [
    {
      title: "Total Employees",
      value: statsData.totalAgents.toString(),
      change: "Active Roster",
      icon: Users,
    },
    {
      title: "Active Sales Reps",
      value: statsData.totalAgents.toString(),
      change: "100%",
      icon: UserCheck,
    },
    {
      title: "Avg Revenue / Rep",
      value: `$${Math.round(statsData.avgRevenue).toLocaleString()}`,
      change: "Per Agent",
      icon: TrendingUp,
    },
    {
      title: "Avg Win Rate",
      value: `${statsData.avgWinRate}%`,
      change: "Team Average",
      icon: UserX,
    },
  ];

  return (
    <div className="employees-page">
      {/* PAGE HEADER */}
      <div className="page-title">
        <div>
          <h1>Employees</h1>
          <p>Manage sales team members and monitor performance metrics across offices</p>
        </div>
      </div>

      {/* STATS */}
      <div className="stats-grid">
        {statCards.map((stat) => {
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

      {/* EMPLOYEE TABLE */}
      <div className="employees-table-card">
        {/* TOOLBAR */}
        <div className="employees-toolbar">
          <div className="employees-search">
            <Search size={17} />
            <input
              type="text"
              placeholder="Search employees by name, email or office..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <select
            className="employees-filter"
            value={regionalOffice}
            onChange={(e) => setRegionalOffice(e.target.value)}
          >
            <option value="All">All Regional Offices</option>
            {regionalOffices.map((office) => (
              <option key={office} value={office}>
                {office} Office
              </option>
            ))}
          </select>
        </div>

        {/* TABLE */}
        <div className="employees-table-wrapper">
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", padding: "40px", color: "#6366f1" }}>
              <Loader2 className="animate-spin" size={24} />
            </div>
          ) : (
            <table className="employees-table">
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Department & Office</th>
                  <th>Revenue</th>
                  <th>Performance</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {agents.map((agent) => (
                  <tr key={agent.id}>
                    <td>
                      <div className="employee-info">
                        <div className="employee-avatar">
                          {agent.name.charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <strong>{agent.name}</strong>
                          <span>{agent.email}</span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div style={{ display: "flex", flexDirection: "column", gap: "2px" }}>
                        <span className="department-badge">{agent.department}</span>
                        {agent.regionalOffice && (
                          <span style={{ fontSize: "0.75rem", color: "#64748b", display: "flex", alignItems: "center", gap: "3px" }}>
                            <Building2 size={12} /> {agent.regionalOffice} Office
                          </span>
                        )}
                      </div>
                    </td>
                    <td>
                      <strong>${Number(agent.totalRevenue).toLocaleString()}</strong>
                    </td>
                    <td>
                      <div className="employee-performance">
                        <div className="performance-bar">
                          <div
                            className="performance-fill"
                            style={{ width: `${Math.min(100, agent.performanceScore)}%` }}
                          />
                        </div>
                        <span>{Math.round(agent.performanceScore)}%</span>
                      </div>
                    </td>
                    <td>
                      <span className="employee-status employee-active">
                        {agent.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {agents.length === 0 && (
                  <tr>
                    <td colSpan="5" className="employees-empty">
                      No employees found matching selected filters.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}