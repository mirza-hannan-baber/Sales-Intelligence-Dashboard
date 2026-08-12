import { useState, useEffect, useMemo } from "react";
import {
  Target,
  TrendingUp,
  AlertTriangle,
  BriefcaseBusiness,
  Loader2,
  AlertCircle,
} from "lucide-react";
import { ResponsiveContainer, BarChart, Bar, CartesianGrid, XAxis, YAxis, Tooltip } from "recharts";
import { dealsService, predictionsService } from "../services/api";

export default function DealProbability() {
  const [deals, setDeals] = useState([]);
  const [loading, setLoading] = useState(true);
  const [missingModelNotice, setMissingModelNotice] = useState(null);
  const [statusFilter, setStatusFilter] = useState("All");

  useEffect(() => {
    fetchDeals();
  }, [statusFilter]);

  const fetchDeals = async () => {
    setLoading(true);
    try {
      const data = await dealsService.getDeals({
        status: statusFilter,
        pageSize: 200,
      });
      setDeals(data.items || []);
    } catch (err) {
      console.error("Error fetching deals:", err);
    } finally {
      setLoading(false);
    }
  };

  const handleRunPrediction = async () => {
    try {
      const res = await predictionsService.predictDealProbability();
      if (res.status === "model_not_available" || res.code === 501) {
        setMissingModelNotice(res);
      }
    } catch (err) {
      console.error("Error invoking deal probability endpoint:", err);
    }
  };

  const avgProbability = useMemo(() => {
    if (!deals.length) return 0;
    return Math.round((deals.reduce((s, d) => s + Number(d.probability || 0), 0) / deals.length) * 10) / 10;
  }, [deals]);

  const probabilityDistribution = [
    { range: "0-20%", deals: deals.filter((d) => d.probability <= 20).length },
    { range: "20-40%", deals: deals.filter((d) => d.probability > 20 && d.probability <= 40).length },
    { range: "40-60%", deals: deals.filter((d) => d.probability > 40 && d.probability <= 60).length },
    { range: "60-80%", deals: deals.filter((d) => d.probability > 60 && d.probability <= 80).length },
    { range: "80-100%", deals: deals.filter((d) => d.probability > 80).length },
  ];

  return (
    <div className="deal-probability-page">
      <div className="page-title">
        <div>
          <h1>Deal Win Probability</h1>
          <p>Pipeline win likelihood from live deals (ML model pending)</p>
        </div>
        <div style={{ display: "flex", gap: "10px", alignItems: "center" }}>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            style={{ padding: "8px 12px", borderRadius: "8px", border: "1px solid #cbd5e1" }}
          >
            <option value="All">All Status</option>
            <option value="In Progress">In Progress</option>
            <option value="Won">Won</option>
            <option value="Lost">Lost</option>
          </select>
          <button className="primary-button" onClick={handleRunPrediction}>
            <Target size={17} />
            Run Deal Prediction
          </button>
        </div>
      </div>

      {missingModelNotice && (
        <div style={{ background: "#fffbeb", border: "1px solid #fcd34d", borderRadius: "10px", padding: "16px 20px", marginBottom: "24px", color: "#92400e" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "10px", fontWeight: "700", fontSize: "1rem", marginBottom: "6px" }}>
            <AlertCircle size={20} color="#d97706" />
            <span>Model Not Yet Available — Prediction Coming Soon</span>
          </div>
          <p style={{ margin: "4px 0 8px 0", fontSize: "0.875rem", color: "#78350f" }}>
            {missingModelNotice.message}
          </p>
          <div style={{ fontSize: "0.8rem", background: "#fef3c7", padding: "8px 12px", borderRadius: "6px", fontFamily: "monospace" }}>
            Drop-in model path: <strong>{missingModelNotice.expected_model_path || "Models and Files/deal_probability_model.pkl"}</strong>
          </div>
        </div>
      )}

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-card-top">
            <div className="stat-icon"><BriefcaseBusiness size={19} /></div>
            <span className="stat-change">Current</span>
          </div>
          <p>Total Pipeline Deals</p>
          <h2>{deals.length}</h2>
        </div>
        <div className="stat-card">
          <div className="stat-card-top">
            <div className="stat-icon"><Target size={19} /></div>
            <span className="stat-change">From DB</span>
          </div>
          <p>Avg Win Probability</p>
          <h2>{avgProbability}%</h2>
        </div>
        <div className="stat-card">
          <div className="stat-card-top">
            <div className="stat-icon"><TrendingUp size={19} /></div>
            <span className="stat-change">High</span>
          </div>
          <p>High Win Probability</p>
          <h2>{deals.filter((d) => d.probability >= 80).length}</h2>
        </div>
        <div className="stat-card">
          <div className="stat-card-top">
            <div className="stat-icon"><AlertTriangle size={19} /></div>
            <span className="stat-change">Attention</span>
          </div>
          <p>At Risk Deals</p>
          <h2>{deals.filter((d) => d.probability < 50).length}</h2>
        </div>
      </div>

      <div className="deal-probability-chart-card">
        <div className="chart-header">
          <div>
            <h3>Deal Probability Distribution</h3>
            <p>Distribution of deals across win probability bands</p>
          </div>
        </div>
        <div className="deal-probability-chart">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={probabilityDistribution} margin={{ top: 10, right: 20, left: 10, bottom: 10 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="range" tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
              <Tooltip formatter={(value) => [`${value} Deals`, "Deals"]} />
              <Bar dataKey="deals" name="Deals" fill="#4f46e5" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="deal-probability-table-card">
        <div className="small-card-header">
          <div>
            <h3>Deal Win Probability Table</h3>
            <p>Live pipeline opportunities with estimated win rates</p>
          </div>
        </div>
        <div className="deal-table-wrapper">
          {loading ? (
            <div style={{ display: "flex", justifyContent: "center", padding: "40px", color: "#6366f1" }}>
              <Loader2 className="animate-spin" size={24} />
            </div>
          ) : (
            <table className="deal-probability-table">
              <thead>
                <tr>
                  <th>Deal</th>
                  <th>Company</th>
                  <th>Owner</th>
                  <th>Deal Value</th>
                  <th>Win Probability</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {deals.slice(0, 25).map((deal) => (
                  <tr key={deal.id}>
                    <td><strong>{deal.name}</strong></td>
                    <td>{deal.company}</td>
                    <td>{deal.owner}</td>
                    <td>${Number(deal.value).toLocaleString()}</td>
                    <td>
                      <div className="probability-cell">
                        <div className="probability-bar">
                          <div className="probability-bar-fill" style={{ width: `${deal.probability}%` }} />
                        </div>
                        <span>{deal.probability}%</span>
                      </div>
                    </td>
                    <td>
                      <span className={`deal-status ${deal.probability >= 70 ? "status-high" : deal.probability >= 50 ? "status-likely" : "status-risk"}`}>
                        {deal.probability >= 70 ? "High Probability" : deal.probability >= 50 ? "Likely" : "At Risk"}
                      </span>
                    </td>
                  </tr>
                ))}
                {deals.length === 0 && (
                  <tr>
                    <td colSpan="6" style={{ textAlign: "center", padding: "24px" }}>No deals found.</td>
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
