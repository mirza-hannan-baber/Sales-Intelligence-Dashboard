import { useState, useEffect } from "react";
import {
  Users,
  DollarSign,
  TrendingUp,
  Target,
  Loader2,
  BrainCircuit,
} from "lucide-react";
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  CartesianGrid,
  XAxis,
  YAxis,
  Tooltip,
} from "recharts";
import { agentsService, predictionsService } from "../services/api";
import { useDataset } from "../context/DatasetContext";

export default function EmployeeRevenue() {
  const { selectedDatasetId } = useDataset();
  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedAgentId, setSelectedAgentId] = useState("");
  const [predictionResult, setPredictionResult] = useState(null);
  const [predicting, setPredicting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchAgents();
  }, [selectedDatasetId]);

  const fetchAgents = async () => {
    setLoading(true);
    try {
      const data = await agentsService.getAgents({ datasetId: selectedDatasetId });
      const items = data.items || [];
      setAgents(items);
      if (items.length > 0) {
        setSelectedAgentId(String(items[0].id));
      }
    } catch (err) {
      console.error("Error fetching employee revenue:", err);
      setError("Failed to load agents from API.");
    } finally {
      setLoading(false);
    }
  };

  const handleRunPrediction = async () => {
    if (!selectedAgentId) return;
    const agent = agents.find((a) => String(a.id) === String(selectedAgentId));
    if (!agent) return;

    setPredicting(true);
    setError(null);
    try {
      const result = await predictionsService.predictEmployeeRevenue({
        salesAgent: agent.name,
        datasetId: selectedDatasetId,
      });

      if (result.error || result.detail) {
        setError(
          result.error ||
            result.detail ||
            "ML prediction service is currently unavailable. Please check the ML service."
        );
        return;
      }
      setPredictionResult(result);
    } catch (err) {
      setError("ML prediction service is currently unavailable. Please check the ML service.");
      console.error("Error predicting employee revenue:", err);
    } finally {
      setPredicting(false);
    }
  };

  const chartData = agents.slice(0, 10).map((a) => ({
    employee: a.name.split(" ")[0],
    fullName: a.name,
    revenue: a.totalRevenue,
  }));

  const totalRevenueVal = agents.reduce((sum, a) => sum + a.totalRevenue, 0);
  const topPerformer = agents.length > 0 ? agents[0] : null;
  const avgRevenue = agents.length > 0 ? totalRevenueVal / agents.length : 0;
  // Target attainment vs top performer as benchmark
  const targetPct =
    topPerformer && topPerformer.totalRevenue > 0
      ? Math.min(100, Math.round((avgRevenue / topPerformer.totalRevenue) * 1000) / 10)
      : 0;

  const formatMoney = (v) => {
    if (v >= 1000000) return `$${(v / 1000000).toFixed(2)}M`;
    return `$${(v / 1000).toFixed(0)}K`;
  };

  const getStatus = (emp) => {
    if (!avgRevenue) return "On Track";
    if (emp.totalRevenue >= avgRevenue * 1.1) return "Above Target";
    if (emp.totalRevenue >= avgRevenue * 0.85) return "On Track";
    return "Below Target";
  };

  const statCards = [
    {
      title: "Total Employee Revenue",
      value: formatMoney(totalRevenueVal),
      change: `${agents.length} agents`,
      icon: DollarSign,
    },
    {
      title: "Top Performer",
      value: topPerformer ? topPerformer.name : "N/A",
      change: topPerformer ? formatMoney(topPerformer.totalRevenue) : "$0",
      icon: Users,
    },
    {
      title: "Average Revenue",
      value: formatMoney(avgRevenue),
      change: "Per Employee",
      icon: TrendingUp,
    },
    {
      title: "Team vs Top",
      value: `${targetPct}%`,
      change: "Avg / top performer",
      icon: Target,
    },
  ];

  if (loading) {
    return (
      <div style={{ display: "flex", justifyContent: "center", alignItems: "center", minHeight: "400px", color: "#6366f1" }}>
        <Loader2 className="animate-spin" size={32} />
        <span style={{ marginLeft: "12px" }}>Loading employee revenue...</span>
      </div>
    );
  }

  return (
    <div className="employee-revenue-page">
      <div className="page-title">
        <div>
          <h1>Employee Revenue</h1>
          <p>Analyze revenue contribution & run AI forecasts from live agent deal history</p>
        </div>
      </div>

      {error && (
        <div style={{ background: "#fef2f2", border: "1px solid #fecaca", color: "#991b1b", padding: "12px 16px", borderRadius: "8px", marginBottom: "16px" }}>
          {error}
        </div>
      )}

      <div style={{ background: "#1e293b", color: "#fff", padding: "16px 20px", borderRadius: "12px", marginBottom: "24px", display: "flex", flexWrap: "wrap", alignItems: "center", justifyContent: "space-between", gap: "12px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
          <BrainCircuit color="#818cf8" size={24} />
          <div>
            <h4 style={{ margin: 0, color: "#f8fafc" }}>Employee Revenue Predictor</h4>
            <span style={{ fontSize: "0.8rem", color: "#94a3b8" }}>Uses agent monthly deal history as model inputs</span>
          </div>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          <select
            value={selectedAgentId}
            onChange={(e) => setSelectedAgentId(e.target.value)}
            style={{ padding: "8px 12px", borderRadius: "6px", background: "#0f172a", color: "#fff", border: "1px solid #334155" }}
          >
            {agents.map((ag) => (
              <option key={ag.id} value={ag.id}>{ag.name}</option>
            ))}
          </select>
          <button
            onClick={handleRunPrediction}
            disabled={predicting || !selectedAgentId}
            className="primary-button"
            style={{ display: "flex", alignItems: "center", gap: "8px" }}
          >
            {predicting ? <Loader2 className="animate-spin" size={16} /> : <BrainCircuit size={16} />}
            Predict Revenue
          </button>
        </div>
      </div>

      {predictionResult && (
        <div style={{ background: "#e0e7ff", border: "1px solid #a5b4fc", borderRadius: "8px", padding: "16px", marginBottom: "24px", color: "#1e1b4b" }}>
          <h3 style={{ margin: "0 0 8px 0" }}>Revenue Forecast for {predictionResult.sales_agent}</h3>
          <div style={{ display: "flex", gap: "24px", flexWrap: "wrap" }}>
            <div>
              <span style={{ fontSize: "0.85rem", color: "#4338ca" }}>Predicted Next Year Revenue:</span>
              <div style={{ fontSize: "1.5rem", fontWeight: "700", color: "#3730a3" }}>
                ${Number(predictionResult.predicted_revenue).toLocaleString()}
              </div>
            </div>
            {/* <div>
              <span style={{ fontSize: "0.85rem", color: "#4338ca" }}>Model Used:</span>
              <div style={{ fontSize: "1rem", fontWeight: "600", color: "#312e81" }}>{predictionResult.model_used}</div>
            </div> */}
          </div>
          {predictionResult.inputs && (
            <div style={{ marginTop: "12px", fontSize: "0.8rem", color: "#4338ca" }}>
              <span style={{ fontWeight: 600 }}>Features used: </span>
              revenue ${Number(predictionResult.inputs.revenue).toLocaleString()} ·{" "}
              {predictionResult.inputs.years_active} yrs active ·{" "}
              {predictionResult.inputs.deals_worked} deals worked ·{" "}
              {(Number(predictionResult.inputs.win_rate) * 100).toFixed(1)}% win rate
            </div>
          )}
        </div>
      )}

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

      <div className="employee-revenue-chart-card">
        <div className="chart-header">
          <div>
            <h3>Revenue by Employee</h3>
            <p>Live revenue generated per rep from seeded CRM deals</p>
          </div>
        </div>

        <div className="employee-revenue-chart">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ top: 10, right: 20, left: 10, bottom: 10 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="employee" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} tickFormatter={(value) => `$${value / 1000}K`} />
              <Tooltip formatter={(value, _n, props) => [`$${Number(value).toLocaleString()}`, props.payload.fullName]} />
              <Bar dataKey="revenue" name="Revenue" fill="#4f46e5" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="employee-revenue-bottom-grid">
        <div className="employee-revenue-card">
          <div className="small-card-header">
            <div>
              <h3>Top Revenue Performers</h3>
              <p>Employees ranked by revenue</p>
            </div>
          </div>

          <div className="employee-ranking">
            {agents.slice(0, 5).map((emp, index) => (
              <div className="employee-ranking-row" key={emp.id}>
                <div className="employee-rank">#{index + 1}</div>
                <div className="employee-ranking-info">
                  <strong>{emp.name}</strong>
                  <span>{emp.wonDeals} won / {emp.totalDeals} deals</span>
                </div>
                <strong>${Number(emp.totalRevenue).toLocaleString()}</strong>
              </div>
            ))}
          </div>
        </div>

        <div className="employee-revenue-card">
          <div className="small-card-header">
            <div>
              <h3>Employee Revenue Details</h3>
              <p>Status vs team average</p>
            </div>
          </div>

          <div className="forecast-table-wrapper">
            <table className="forecast-table">
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Revenue</th>
                  <th>Win Rate</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {agents.slice(0, 10).map((emp) => (
                  <tr key={emp.id}>
                    <td>{emp.name}</td>
                    <td>${Number(emp.totalRevenue).toLocaleString()}</td>
                    <td>{emp.winRate}%</td>
                    <td>
                      <span className="revenue-status">{getStatus(emp)}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
