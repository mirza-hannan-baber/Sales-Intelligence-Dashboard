import React from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
  Legend
} from 'recharts';

function RevenueTrendChart({ historicalTrend = [] }) {
  const formatMonth = (tick) => {
    if (!tick || typeof tick !== 'string' || !tick.includes('-')) return tick;
    const [year, month] = tick.split('-');
    const monthNames = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    const monthIndex = parseInt(month, 10) - 1;
    const mName = monthNames[monthIndex] || month;
    return `${mName} '${year ? year.slice(2) : ''}`;
  };

  return (
    <div
      style={{
        background: '#fff',
        padding: '16px',
        borderRadius: '12px',
        border: '1px solid #e2e8f0',
        height: '100%',
        boxSizing: 'border-box'
      }}
    >
      <h3
        style={{
          fontSize: '15px',
          fontWeight: 'bold',
          marginTop: 0,
          marginBottom: '12px',
          color: '#1e293b'
        }}
      >
        Revenue Trend: Actual vs Predicted
      </h3>

      <div style={{ width: '100%', height: 220 }}>
        <ResponsiveContainer width="100%" height="100%">
          <LineChart
            data={historicalTrend}
            margin={{ top: 5, right: 10, left: 0, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" vertical={false} />

            <XAxis
              dataKey="month"
              tickFormatter={formatMonth}
              tick={{ fontSize: 10, fill: '#64748b' }}
              dy={6}
            />

            <YAxis
              tickFormatter={(val) => `$${(val / 1000).toFixed(0)}k`}
              tick={{ fontSize: 10, fill: '#64748b' }}
              width={40}
            />

            <Tooltip
              formatter={(value) => [`$${value.toLocaleString()}`, '']}
            />

            <Legend wrapperStyle={{ fontSize: '11px', paddingTop: '10px' }} />

            <Line
              type="monotone"
              dataKey="ActualRevenue"
              name="Actual Revenue"
              stroke="#2563eb"
              strokeWidth={2.5}
              dot={false}
            />

            <Line
              type="monotone"
              dataKey="PredictedRevenue"
              name="Predicted Revenue"
              stroke="#f97316"
              strokeDasharray="5 5"
              strokeWidth={2}
              dot={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

export default RevenueTrendChart;