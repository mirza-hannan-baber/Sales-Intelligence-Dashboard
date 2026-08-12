import { useState, useEffect } from "react";
import {
  TrendingUp,
  DollarSign,
  Target,
  BrainCircuit,
  Loader2,
} from "lucide-react";
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from "recharts";
import { dashboardService } from "../services/api";
import { useAuth } from "../context/AuthContext";

export default function Dashboard() {
  const { user } = useAuth();
  const [data, setData] = useState(null);
  const [months, setMonths] = useState(6);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchDashboardData(months);
  }, [months]);

  const fetchDashboardData = async (range = months) => {
    setLoading(true);
    setError(null);
    try {
      const kpiData = await dashboardService.getKpis({ months: range });
      setData(kpiData);
    } catch (err) {
      setError("Failed to load live dashboard metrics from backend API.");
    } finally {
      setLoading(false);
    }
  };

  if (loading && !data) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "400px", color: "#6366f1" }}>
        <Loader2 className="animate-spin" size={32} />
        <span style={{ marginLeft: "12px", fontSize: "1rem" }}>Loading live dashboard...</span>
      </div>
    );
  }

  if (error && !data) {
    return (
      <div className="dashboard-page">
        <div className="page-title">
          <div>
            <h1>Dashboard</h1>
            <p style={{ color: "#b91c1c" }}>{error}</p>
          </div>
          <button className="primary-button" onClick={() => fetchDashboardData(months)}>
            Retry
          </button>
        </div>
      </div>
    );
  }

  const stats = [
    {
      title: "Total Revenue",
      value: data?.totalRevenue || "$0",
      change: data?.revenueChange || "0%",
      icon: DollarSign,
    },
    {
      title: "Predicted Revenue",
      value: data?.predictedRevenue || "$0",
      change: data?.predictedChange || "0%",
      icon: TrendingUp,
    },
    {
      title: "Win Rate",
      value: data?.winRate || "0%",
      change: data?.winRateChange || "0%",
      icon: Target,
    },
    {
      title: "AI Confidence",
      value: data?.aiConfidence || "0%",
      change: `${data?.wonDeals || 0} won deals`,
      icon: BrainCircuit,
    },
  ];

  const revenueChart = data?.revenueChart || [];
  const winRateChart = data?.winRateChart || [];
  const winDomainMin = Math.max(0, Math.floor(Math.min(...winRateChart.map((d) => d.winRate), 100) / 10) * 10 - 10);

  return (
    <div className="dashboard-page">
      <div className="page-title">
        <div>
          <h1>Dashboard</h1>
          <p>
            Welcome back, {user?.fullName || "Admin"}. Live overview from CRM database
            ({data?.totalDeals || 0} deals).
          </p>
        </div>
        <button className="primary-button" onClick={() => fetchDashboardData(months)} disabled={loading}>
          {loading ? <Loader2 className="animate-spin" size={16} /> : null}
          Refresh Live Data
        </button>
      </div>

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

      <div className="dashboard-chart-card">
        <div className="chart-header">
          <div>
            <h3>Revenue Overview</h3>
            <p>Actual vs model-style predicted revenue from database deals</p>
          </div>
          <select
            value={months}
            onChange={(e) => setMonths(Number(e.target.value))}
            aria-label="Time range filter"
          >
            <option value={6}>Last 6 Months</option>
            <option value={12}>Last 12 Months</option>
          </select>
        </div>

        <div className="revenue-chart">
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100%", color: "#6366f1" }}>
              <Loader2 className="animate-spin" size={28} />
            </div>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart
                data={revenueChart}
                margin={{ top: 10, right: 10, left: 10, bottom: 10 }}
              >
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                <YAxis
                  tick={{ fontSize: 11 }}
                  tickFormatter={(value) =>
                    value >= 1000000 ? `$${(value / 1000000).toFixed(1)}M` : `$${Math.round(value / 1000)}K`
                  }
                />
                <Tooltip
                  formatter={(value, name) => [
                    `$${Number(value).toLocaleString()}`,
                    name === "actualRevenue" ? "Actual Revenue" : "Predicted Revenue",
                  ]}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="actualRevenue"
                  name="Actual Revenue"
                  stroke="#4f46e5"
                  strokeWidth={3}
                  connectNulls={false}
                  dot={{ r: 4 }}
                  activeDot={{ r: 7 }}
                />
                <Line
                  type="monotone"
                  dataKey="predictedRevenue"
                  name="Predicted Revenue"
                  stroke="#94a3b8"
                  strokeWidth={3}
                  strokeDasharray="6 5"
                  dot={{ r: 4 }}
                  activeDot={{ r: 7 }}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      <div className="dashboard-bottom-grid">
        <div className="dashboard-small-card">
          <div className="small-card-header">
            <div>
              <h3>Win Rate Trend</h3>
              <p>Monthly closed-deal win rate (%)</p>
            </div>
          </div>

          <div className="win-rate-chart">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart
                data={winRateChart}
                margin={{ top: 10, right: 10, left: 0, bottom: 5 }}
              >
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="month" tick={{ fontSize: 11 }} />
                <YAxis
                  domain={[winDomainMin, 100]}
                  tick={{ fontSize: 11 }}
                  tickFormatter={(value) => `${value}%`}
                />
                <Tooltip formatter={(value) => [`${value}%`, "Win Rate"]} />
                <Line
                  type="monotone"
                  dataKey="winRate"
                  name="Win Rate"
                  stroke="#4f46e5"
                  strokeWidth={3}
                  dot={{ r: 4 }}
                  activeDot={{ r: 7 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="dashboard-small-card">
          <div className="small-card-header">
            <div>
              <h3>System Status</h3>
              <p>Live architecture status</p>
            </div>
          </div>
          <div style={{ padding: "16px", fontSize: "0.875rem", color: "#64748b" }}>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ .NET 9 Web API connected</p>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ SQLite Database ({data?.totalDeals || 0} deals)</p>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ Chart range: last {months} months</p>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ Role: {user?.role || "User"}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
