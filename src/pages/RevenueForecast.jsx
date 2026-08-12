import { useState, useEffect } from "react";
import {
  TrendingUp,
  DollarSign,
  Target,
  BrainCircuit,
  Loader2,
  Play,
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
import { dashboardService, predictionsService } from "../services/api";

export default function RevenueForecast() {
  const [lag1, setLag1] = useState(0);
  const [lag2, setLag2] = useState(0);
  const [lag3, setLag3] = useState(0);
  const [rollingMean, setRollingMean] = useState(0);
  const [predicting, setPredicting] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [forecastResult, setForecastResult] = useState(null);
  const [chartData, setChartData] = useState([]);
  const [aiConfidence, setAiConfidence] = useState("0%");

  useEffect(() => {
    loadBaseline();
  }, []);

  const loadBaseline = async () => {
    setLoading(true);
    setError(null);
    try {
      const kpi = await dashboardService.getKpis({ months: 6 });
      const l1 = Number(kpi.lag1) || 0;
      const l2 = Number(kpi.lag2) || 0;
      const l3 = Number(kpi.lag3) || 0;
      const rm = Number(kpi.rollingMean) || Math.round((l1 + l2 + l3) / 3);

      setLag1(l1);
      setLag2(l2);
      setLag3(l3);
      setRollingMean(rm);
      setAiConfidence(kpi.aiConfidence || "0%");

      const history = (kpi.revenueChart || [])
        .filter((r) => Number(r.actualRevenue) > 0)
        .slice(-3)
        .map((r) => ({
          month: r.month,
          actualRevenue: Number(r.actualRevenue),
          predictedRevenue: null,
        }));

      setChartData(history);
      setForecastResult(null);
    } catch (err) {
      setError("Could not load revenue history from API.");
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleGenerateForecast = async (e) => {
    e?.preventDefault();
    setPredicting(true);
    setError(null);
    try {
      const res = await predictionsService.predictCompanyRevenue({
        lag1: Number(lag1),
        lag2: Number(lag2),
        lag3: Number(lag3),
        rollingMean: Number(rollingMean),
      });

      if (res.error || res.detail) {
        setError(res.error || res.detail || "Prediction service unavailable.");
        return;
      }

      setForecastResult(res);

      setChartData([
        { month: "Lag 3", actualRevenue: Number(lag3), predictedRevenue: null },
        { month: "Lag 2", actualRevenue: Number(lag2), predictedRevenue: null },
        { month: "Lag 1", actualRevenue: Number(lag1), predictedRevenue: null },
        {
          month: "Next (Fcst)",
          actualRevenue: null,
          predictedRevenue: Number(res.predicted_revenue),
        },
      ]);
    } catch (err) {
      setError("Revenue prediction failed. Is the ML service running on port 5001?");
      console.error("Error generating revenue forecast:", err);
    } finally {
      setPredicting(false);
    }
  };

  const predicted = forecastResult?.predicted_revenue ?? 0;
  const growth =
    lag1 > 0 && predicted
      ? (((predicted - lag1) / lag1) * 100).toFixed(1)
      : "0.0";

  const stats = [
    {
      title: "Current Revenue",
      value: lag1 ? `$${(lag1 / 1000).toFixed(0)}K` : "$0",
      change: "Latest month (from DB)",
      icon: DollarSign,
    },
    {
      title: "Predicted Revenue",
      value: predicted ? `$${(predicted / 1000).toFixed(0)}K` : "—",
      change: forecastResult ? "AI model output" : "Run prediction",
      icon: TrendingUp,
    },
    {
      title: "Expected Growth",
      value: forecastResult ? `${growth >= 0 ? "+" : ""}${growth}%` : "—",
      change: Number(growth) >= 0 ? "Positive" : "Negative",
      icon: Target,
    },
    {
      title: "AI Confidence",
      value: aiConfidence,
      change: "From data volume",
      icon: BrainCircuit,
    },
  ];

  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "400px", color: "#6366f1" }}>
        <Loader2 className="animate-spin" size={32} />
        <span style={{ marginLeft: "12px" }}>Loading revenue history...</span>
      </div>
    );
  }

  return (
    <div className="revenue-forecast-page">
      <div className="page-title">
        <div>
          <h1>Revenue Forecast</h1>
          <p>Predict future revenue using the trained Random Forest model (lags pre-filled from live DB)</p>
        </div>
        <button className="secondary-button" onClick={loadBaseline}>Reload from DB</button>
      </div>

      {error && (
        <div style={{ background: "#fef2f2", border: "1px solid #fecaca", color: "#991b1b", padding: "12px 16px", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      <div style={{ background: "#1e293b", color: "#fff", padding: "20px", borderRadius: "12px", marginBottom: "24px" }}>
        <h3 style={{ margin: "0 0 16px 0", color: "#f8fafc", fontSize: "1.1rem" }}>
          Interactive Forecast Parameters
        </h3>
        <form onSubmit={handleGenerateForecast} style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: "16px", alignItems: "end" }}>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>Lag 1 Revenue ($)</label>
            <input type="number" value={lag1} onChange={(e) => setLag1(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>Lag 2 Revenue ($)</label>
            <input type="number" value={lag2} onChange={(e) => setLag2(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>Lag 3 Revenue ($)</label>
            <input type="number" value={lag3} onChange={(e) => setLag3(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>Rolling 3M Mean ($)</label>
            <input type="number" value={rollingMean} onChange={(e) => setRollingMean(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <button type="submit" disabled={predicting} className="primary-button" style={{ width: "100%", height: "38px", display: "flex", justifyContent: "center", alignItems: "center", gap: "8px" }}>
              {predicting ? <Loader2 className="animate-spin" size={16} /> : <Play size={16} />}
              Predict Revenue
            </button>
          </div>
        </form>
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

      <div className="forecast-chart-card">
        <div className="chart-header">
          <div>
            <h3>Revenue Forecast Chart</h3>
            <p>Historical revenue from DB vs live AI model prediction</p>
          </div>
        </div>

        <div className="forecast-chart">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 10, right: 15, left: 10, bottom: 10 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} tickFormatter={(value) => `$${value / 1000}K`} />
              <Tooltip formatter={(value, name) => [`$${Number(value).toLocaleString()}`, name === "actualRevenue" ? "Actual Revenue" : "Predicted Revenue"]} />
              <Legend />
              <Line type="monotone" dataKey="actualRevenue" name="Actual Revenue" stroke="#4f46e5" strokeWidth={3} connectNulls={false} dot={{ r: 5 }} />
              <Line type="monotone" dataKey="predictedRevenue" name="Predicted Revenue" stroke="#10b981" strokeWidth={3} strokeDasharray="6 5" connectNulls={false} dot={{ r: 7 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}
