import { TrendingUp, BarChart2, CheckCircle2, AlertCircle } from 'lucide-react';

export function ForecastCard({ data }) {
  if (!data) return null;
  const forecastList = data.forecast || data.Forecast || [];
  const nextQuarterTotal = data.nextQuarterTotal || data.NextQuarterTotal || 0;

  return (
    <div className="chat-card forecast-result-card">
      <div className="chat-card-header">
        <TrendingUp size={16} color="#818cf8" />
        <span>Next Quarter Revenue Forecast</span>
      </div>
      <div className="forecast-items">
        {forecastList.map((item, idx) => (
          <div key={idx} className="forecast-item-row">
            <span className="forecast-month">{item.month || item.Month}</span>
            <span className="forecast-val">${Number(item.predictedRevenue || item.PredictedRevenue || 0).toLocaleString()}</span>
          </div>
        ))}
      </div>
      {nextQuarterTotal > 0 && (
        <div className="forecast-total-row">
          <span>Next Quarter Total</span>
          <strong>${Number(nextQuarterTotal).toLocaleString()}</strong>
        </div>
      )}
    </div>
  );
}

export function CorrelationCard({ data }) {
  if (!Array.isArray(data) || data.length === 0) return null;

  return (
    <div className="chat-card correlation-result-card">
      <div className="chat-card-header">
        <BarChart2 size={16} color="#34d399" />
        <span>Factor Correlation to Win Rate</span>
      </div>
      <table className="chat-card-table">
        <thead>
          <tr>
            <th>Factor</th>
            <th>Relationship Strength</th>
          </tr>
        </thead>
        <tbody>
          {data.map((item, idx) => (
            <tr key={idx}>
              <td>{item.factor || item.Factor}</td>
              <td style={{ fontWeight: 600, color: (item.correlation || item.Correlation) > 0 ? '#10b981' : '#ef4444' }}>
                {(item.correlation || item.Correlation) > 0 ? '+' : ''}
                {item.correlation || item.Correlation}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function RecommendationCard({ data }) {
  if (!data) return null;

  return (
    <div className="chat-card recommendation-result-card">
      <div className="chat-card-header">
        <CheckCircle2 size={16} color="#f59e0b" />
        <span>Calculated Performance Insights</span>
      </div>
      <ul className="recommendation-list">
        <li>
          <strong>Top Win Rate Industry:</strong> {data.highestWinRateIndustry || data.HighestWinRateIndustry} ({data.highestWinRate || data.HighestWinRate}%)
        </li>
        <li>
          <strong>Longest Sales Cycle:</strong> {data.longestSalesCycleIndustry || data.LongestSalesCycleIndustry} ({data.averageSalesCycle || data.AverageSalesCycle} days)
        </li>
        <li>
          <strong>Strongest Impact Factor:</strong> {data.strongestFactor || data.StrongestFactor} ({data.correlation || data.Correlation})
        </li>
      </ul>
    </div>
  );
}

export function DataAnalysisCard({ data }) {
  if (!data) return null;

  const employees = data.employees || data.Employees;
  if (Array.isArray(employees) && employees.length > 0) {
    return (
      <div className="chat-card">
        <div className="chat-card-header">
          <BarChart2 size={16} color="#818cf8" />
          <span>Employee Revenue{data.year || data.Year ? ` (${data.year || data.Year})` : ""}</span>
        </div>
        <table className="chat-card-table">
          <thead>
            <tr>
              <th>Employee</th>
              <th>Revenue</th>
            </tr>
          </thead>
          <tbody>
            {employees.slice(0, 10).map((row, idx) => (
              <tr key={idx}>
                <td>{row.salesAgent || row.SalesAgent}</td>
                <td>${Number(row.revenue || row.Revenue || 0).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {(data.totalRevenue || data.TotalRevenue) != null && (
          <div className="forecast-total-row">
            <span>Total</span>
            <strong>${Number(data.totalRevenue || data.TotalRevenue).toLocaleString()}</strong>
          </div>
        )}
      </div>
    );
  }

  if (data.uniqueSalesAgents != null || data.UniqueSalesAgents != null) {
    return (
      <div className="chat-card">
        <div className="chat-card-header">
          <CheckCircle2 size={16} color="#34d399" />
          <span>Verified Data Result</span>
        </div>
        <div className="forecast-items">
          <div className="forecast-item-row">
            <span>Unique sales agents</span>
            <strong>{data.uniqueSalesAgents ?? data.UniqueSalesAgents}</strong>
          </div>
          {(data.totalRevenue || data.TotalRevenue) != null && (
            <div className="forecast-item-row">
              <span>Won revenue</span>
              <strong>${Number(data.totalRevenue || data.TotalRevenue).toLocaleString()}</strong>
            </div>
          )}
          {(data.totalDeals || data.TotalDeals) != null && (
            <div className="forecast-item-row">
              <span>Deal records (not agents)</span>
              <span>{data.totalDeals ?? data.TotalDeals}</span>
            </div>
          )}
        </div>
      </div>
    );
  }

  return null;
}
