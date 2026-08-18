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

  // Colour a badge by direction. A change of null means the comparison is not
  // meaningful for that card, so it renders as neutral context instead.
  const toneOf = (change) => {
    if (!change) return "neutral";
    if (change.startsWith("-")) return "down";
    if (change.startsWith("+") && !/^\+0(\.0)?(%|pp)$/.test(change)) return "up";
    return "neutral";
  };

  const baseline = data?.previousPeriodLabel;
  const period = data?.completeThrough;
  const vsPeriod = baseline && period ? `${period} vs ${baseline}` : "";

  const stats = [
    {
      // Lifetime cumulative: a running total has no meaningful period-over-period
      // % change, so this card carries context rather than a percentage badge.
      title: "Total Revenue",
      value: data?.totalRevenue || "$0",
      change: "3 years of data",
      tone: "neutral",
      icon: DollarSign,
      footnote: period ? `Complete data through ${period}` : "",
    },
    {
      title: "Predicted Revenue",
      value: data?.predictedRevenue || "$0",
      change: data?.predictedChange || "—",
      tone: toneOf(data?.predictedChange),
      icon: TrendingUp,
      footnote: period ? `Next month vs ${period}` : "",
    },
    {
      title: "Win Rate",
      value: data?.winRate || "0%",
      change: data?.winRateChange || "—",
      tone: toneOf(data?.winRateChange),
      icon: Target,
      footnote: vsPeriod,
    },
    {
      title: "AI Confidence",
      value: data?.aiConfidence || "0%",
      change: `${data?.wonDeals || 0} won deals`,
      tone: "neutral",
      icon: BrainCircuit,
      footnote: "",
    },
  ];

  const revenueChart = data?.revenueChart || [];
  const winRateChart = data?.winRateChart || [];
  const winDomainMin = Math.max(0, Math.floor(Math.min(...winRateChart.map((d) => d.winRate), 100) / 10) * 10 - 10);
  const excluded = data?.excludedIncompleteMonths || [];

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
                <span className={`stat-change is-${stat.tone}`}>{stat.change}</span>
              </div>
              <p>{stat.title}</p>
              <h2>{stat.value}</h2>
              {stat.footnote && <span className="stat-footnote">{stat.footnote}</span>}
            </div>
          );
        })}
      </div>

      <div className="dashboard-chart-card">
        <div className="chart-header">
          <div>
            <h3>Revenue Overview</h3>
            <p>
              One continuous line: solid indigo through the last complete month,
              dashed orange for the forecast
              {excluded.length > 0 && ` (excludes still-accumulating ${excluded.join(", ")})`}
            </p>
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
                  formatter={(value, name, item) => {
                    if (value == null) return [null, null];
                    const seriesLabel =
                      item?.dataKey === "actualRevenue" || name === "Actual" || name === "actualRevenue"
                        ? "Actual"
                        : "Predicted";
                    return [`$${Number(value).toLocaleString()}`, seriesLabel];
                  }}
                />
                <Legend />
                {/*
                  One continuous Revenue line rendered as two <Line>s that share
                  the boundary month (that month has BOTH actualRevenue and
                  predictedRevenue). The actual series is null after the boundary;
                  the predicted series is null before it. Result: one unbroken
                  line with a single colour/style change at the actual→forecast
                  handoff, and no visual gap.
                */}
                <Line
                  type="monotone"
                  dataKey="actualRevenue"
                  name="Actual"
                  stroke="#6366f1"
                  strokeWidth={3}
                  connectNulls={false}
                  dot={{ r: 4, fill: "#6366f1" }}
                  activeDot={{ r: 7 }}
                />
                <Line
                  type="monotone"
                  dataKey="predictedRevenue"
                  name="Predicted"
                  stroke="#f97316"
                  strokeWidth={3}
                  strokeDasharray="6 6"
                  connectNulls={false}
                  dot={{ r: 4, fill: "#f97316" }}
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
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ Analysis through charts</p>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ {data?.totalDeals || 0} deals analyzed</p>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ Chart range: last {months} months</p>
            <p style={{ margin: "4px 0", color: "#1e293b", fontWeight: "600" }}>✓ Role: {user?.role || "User"}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
