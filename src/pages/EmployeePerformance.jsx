import { useState, useEffect, useMemo } from "react";
import {
  BarChart,
  Bar,
  CartesianGrid,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { Loader2, BrainCircuit } from "lucide-react";
import { agentsService, predictionsService } from "../services/api";

export default function EmployeePerformance() {
  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedAgentId, setSelectedAgentId] = useState("");
  const [predictionResult, setPredictionResult] = useState(null);
  const [predicting, setPredicting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchAgents();
  }, []);

  const fetchAgents = async () => {
    setLoading(true);
    try {
      const data = await agentsService.getAgents({});
      const items = data.items || [];
      setAgents(items);
      if (items.length > 0) {
        setSelectedAgentId(String(items[0].id));
      }
    } catch (err) {
      console.error("Error fetching agent performance:", err);
      setError("Failed to load agents.");
    } finally {
      setLoading(false);
    }
  };

  const handleRunPrediction = async () => {
    if (!selectedAgentId) return;
    setPredicting(true);
    setError(null);
    try {
      const perf = await agentsService.getAgentPerformance(selectedAgentId);
      const f = perf.predictionFeatures || {};
      const result = await predictionsService.predictEmployeePerformance({
        salesAgent: f.salesAgent,
        totalDealsLag1: Number(f.totalDealsLag1) || 0,
        wonDealsLag1: Number(f.wonDealsLag1) || 0,
        closedDealsLag1: Number(f.closedDealsLag1) || 0,
        totalRevenueLag1: Number(f.totalRevenueLag1) || 0,
        averageDealValueLag1: Number(f.averageDealValueLag1) || 0,
        averageSalesCycleLag1: Number(f.averageSalesCycleLag1) || 30,
        winRateLag1: Number(f.winRateLag1) || 0,
        winRateRolling3: Number(f.winRateRolling3) || 0,
        revenueRolling3: Number(f.revenueRolling3) || 0,
        dealsRolling3: Number(f.dealsRolling3) || 0,
        monthNumber: f.monthNumber || 3,
        quarter: f.quarter || 1,
        year: f.year || 2026,
      });

      if (result.error || result.detail) {
        setError(result.error || result.detail || "Prediction failed.");
        return;
      }
      setPredictionResult(result);
    } catch (err) {
      setError("Performance prediction failed. Check ML service.");
      console.error("Error predicting employee performance:", err);
    } finally {
      setPredicting(false);
    }
  };

  const filteredEmployees = useMemo(() => {
    return agents.filter((emp) =>
      emp.name.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }, [agents, searchTerm]);

  const chartData = filteredEmployees.slice(0, 10).map((emp) => ({
    name: emp.name.split(" ")[0],
    fullName: emp.name,
    performanceScore: Math.round(emp.performanceScore),
  }));

  const totalEmployees = filteredEmployees.length;
  const averageRevenue =
    totalEmployees > 0
      ? filteredEmployees.reduce((sum, e) => sum + e.totalRevenue, 0) / totalEmployees
      : 0;
  const averageWinRate =
    totalEmployees > 0
      ? filteredEmployees.reduce((sum, e) => sum + e.winRate, 0) / totalEmployees
      : 0;
  const topPerformer =
    filteredEmployees.length > 0
      ? [...filteredEmployees].sort((a, b) => b.performanceScore - a.performanceScore)[0]
      : null;

  return (
    <div className="employee-performance-page">
      <div className="page-header" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px" }}>
        <div>
          <h1>Employee Performance</h1>
          <p>Track team performance & run AI predictions from live deal history</p>
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
            <h4 style={{ margin: 0, color: "#f8fafc" }}>AI Performance Predictor</h4>
            <span style={{ fontSize: "0.8rem", color: "#94a3b8" }}>Inputs derived from selected agent’s CRM deals</span>
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
            Predict Performance
          </button>
        </div>
      </div>

      {predictionResult && (
        <div style={{ background: "#e0e7ff", border: "1px solid #a5b4fc", borderRadius: "8px", padding: "16px", marginBottom: "24px", color: "#1e1b4b" }}>
          <h3 style={{ margin: "0 0 8px 0" }}>Prediction Result for {predictionResult.sales_agent}</h3>
          <div style={{ display: "flex", gap: "24px", flexWrap: "wrap" }}>
            <div>
              <span style={{ fontSize: "0.85rem", color: "#4338ca" }}>Predicted Performance Score:</span>
              <div style={{ fontSize: "1.5rem", fontWeight: "700", color: "#3730a3" }}>
                {Number(predictionResult.predicted_performance_score).toFixed(1)}%
              </div>
            </div>
            <div>
              <span style={{ fontSize: "0.85rem", color: "#4338ca" }}>Model Used:</span>
              <div style={{ fontSize: "1rem", fontWeight: "600", color: "#312e81" }}>{predictionResult.model_used}</div>
            </div>
          </div>
        </div>
      )}

      <div className="performance-filters">
        <div className="search-box">
          <input
            type="text"
            placeholder="Search employee..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      <div className="performance-kpi-grid">
        <div className="performance-kpi-card">
          <span>Total Employees</span>
          <strong>{totalEmployees}</strong>
          <small>Active reps</small>
        </div>
        <div className="performance-kpi-card">
          <span>Average Revenue</span>
          <strong>${Math.round(averageRevenue).toLocaleString()}</strong>
          <small>Per rep</small>
        </div>
        <div className="performance-kpi-card">
          <span>Average Win Rate</span>
          <strong>{averageWinRate.toFixed(1)}%</strong>
          <small>Across team</small>
        </div>
        <div className="performance-kpi-card">
          <span>Top Performer</span>
          <strong className="top-performer-name">{topPerformer ? topPerformer.name : "N/A"}</strong>
          <small>{topPerformer ? `${Math.round(topPerformer.performanceScore)} score` : "No data"}</small>
        </div>
      </div>

      <div className="performance-chart-card">
        <div className="section-heading">
          <div>
            <h2>Performance Comparison</h2>
            <p>Employee performance scores from database</p>
          </div>
        </div>
        <div className="performance-chart">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={chartData} margin={{ top: 10, right: 20, left: 0, bottom: 10 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" tick={{ fontSize: 10 }} />
              <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} />
              <Tooltip formatter={(value, _n, props) => [value, props.payload.fullName]} />
              <Bar dataKey="performanceScore" name="Performance Score" fill="#4f46e5" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="employee-table-card">
        <div className="section-heading">
          <div>
            <h2>Employee Performance Details</h2>
            <p>Live agent metrics from backend</p>
          </div>
        </div>
        <div className="employee-table-wrapper">
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", padding: "40px", color: "#6366f1" }}>
              <Loader2 className="animate-spin" size={24} />
            </div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Employee</th>
                  <th>Revenue</th>
                  <th>Deals</th>
                  <th>Won</th>
                  <th>Win Rate</th>
                  <th>Performance</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filteredEmployees.map((employee) => (
                  <tr key={employee.id}>
                    <td>
                      <div className="employee-name">
                        <strong>{employee.name}</strong>
                        <span>{employee.department}</span>
                      </div>
                    </td>
                    <td>${Number(employee.totalRevenue).toLocaleString()}</td>
                    <td>{employee.totalDeals}</td>
                    <td>{employee.wonDeals}</td>
                    <td>{employee.winRate}%</td>
                    <td>
                      <div className="score-cell">
                        <span>{Math.round(employee.performanceScore)}</span>
                        <div className="score-bar">
                          <div style={{ width: `${Math.min(100, employee.performanceScore)}%` }} />
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className="status-badge status-active">{employee.status}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
