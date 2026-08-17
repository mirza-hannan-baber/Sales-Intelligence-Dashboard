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
import { FEATURE_LABELS } from "../utils/featureLabels";

export default function RevenueForecast() {
  const [lag1, setLag1] = useState("250000");
  const [lag2, setLag2] = useState("240000");
  const [lag3, setLag3] = useState("230000");
  const [rollingMean, setRollingMean] = useState("240000");
  const [predicting, setPredicting] = useState(false);
  const [loadingBaseline, setLoadingBaseline] = useState(true);
  const [error, setError] = useState(null);

  const [forecastResult, setForecastResult] = useState(null);
  const [chartData, setChartData] = useState([]);
  const [aiConfidence, setAiConfidence] = useState("0%");

  useEffect(() => {
    loadBaseline();
  }, []);

  const loadBaseline = async () => {
    setLoadingBaseline(true);
    setError(null);
    try {
      const kpi = await dashboardService.getKpis({ months: 6 });
      const l1 = Number(kpi.lag1) || 0;
      const l2 = Number(kpi.lag2) || 0;
      const l3 = Number(kpi.lag3) || 0;
      const rm = Number(kpi.rollingMean) || Math.round((l1 + l2 + l3) / 3);

      setLag1(String(l1));
      setLag2(String(l2));
      setLag3(String(l3));
      setRollingMean(String(rm));
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
      setError("Unable to connect to the backend service. Please make sure the backend is running.");
      console.error(err);
    } finally {
      setLoadingBaseline(false);
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
        setError(
          res.error ||
            res.detail ||
            "ML prediction service is currently unavailable. Please check the ML service."
        );
        return;
      }

      const predictedValue = Number(res.predicted_revenue);
      if (!Number.isFinite(predictedValue)) {
        setError("The forecast model returned an invalid value. Please try again.");
        return;
      }

      setForecastResult(res);

      setChartData([
        { month: FEATURE_LABELS.lag_3, actualRevenue: Number(lag3), predictedRevenue: null },
        { month: FEATURE_LABELS.lag_2, actualRevenue: Number(lag2), predictedRevenue: null },
        // Boundary point: Last Month's Revenue is both the last actual and the start of the forecast,
        // so the two segments join with no gap.
        {
          month: FEATURE_LABELS.lag_1,
          actualRevenue: Number(lag1),
          predictedRevenue: Number(lag1),
        },
        {
          month: "Next (Fcst)",
          actualRevenue: null,
          predictedRevenue: predictedValue,
        },
      ]);
    } catch (err) {
      setError("ML prediction service is currently unavailable. Please check the ML service.");
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

  if (loadingBaseline) {
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
          <p>Predict future revenue using the trained lag revenue model (lags pre-filled from live DB)</p>
        </div>
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
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>{FEATURE_LABELS.lag_1} ($)</label>
            <input type="number" value={lag1} onChange={(e) => setLag1(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>{FEATURE_LABELS.lag_2} ($)</label>
            <input type="number" value={lag2} onChange={(e) => setLag2(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>{FEATURE_LABELS.lag_3} ($)</label>
            <input type="number" value={lag3} onChange={(e) => setLag3(e.target.value)} style={{ width: "100%", padding: "8px 12px", borderRadius: "6px", background: "#0f172a", border: "1px solid #334155", color: "#fff" }} />
          </div>
          <div>
            <label style={{ display: "block", fontSize: "0.8rem", color: "#cbd5e1", marginBottom: "4px" }}>{FEATURE_LABELS.rolling_mean_3} ($)</label>
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
            <p>One continuous line — solid indigo for actual, dashed orange for predicted</p>
          </div>
        </div>

        <div className="forecast-chart">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 10, right: 15, left: 10, bottom: 10 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} tickFormatter={(value) => `$${Number(value || 0) / 1000}K`} />
              <Tooltip
                formatter={(value, name, item) => {
                  if (value == null) return [null, null];
                  const seriesName = item?.dataKey === "actualRevenue" || name === "Actual" || name === "actualRevenue" ? "Actual" : "Predicted";
                  return [
                    `$${Number(value).toLocaleString()}`,
                    seriesName,
                  ];
                }}
              />
              <Legend />
              <Line type="monotone" dataKey="actualRevenue" name="Actual" stroke="#6366f1" strokeWidth={3} connectNulls={false} dot={{ r: 5, fill: "#6366f1" }} />
              <Line type="monotone" dataKey="predictedRevenue" name="Predicted" stroke="#f97316" strokeWidth={3} strokeDasharray="6 6" connectNulls={false} dot={{ r: 5, fill: "#f97316" }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
}

