import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid,
  Legend
} from 'recharts';

function IndustryWinRateChart({ sectorData = [] }) {
  return (
    <div
      style={{
        background: '#fff',
        padding: '20px',
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
          marginBottom: '16px',
          color: '#1e293b'
        }}
      >
        Win Rate by Industry: Current vs Predicted
      </h3>

      <div style={{ width: '100%', height: 300 }}>
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={sectorData} layout="vertical">
            <CartesianGrid strokeDasharray="3 3" horizontal={false} />

            <XAxis type="number" unit="%" domain={[0, 100]} />

            <YAxis
              style={{ fontSize: '11px' }}
              dataKey="sector"
              type="category"
              width={90}
              tick={{ fill: '#64748b' }}
            />

            <Tooltip formatter={(value) => [`${value}%`]} />

            <Legend wrapperStyle={{ fontSize: '12px', paddingTop: '8px' }} />

            <Bar
              dataKey="CurrentWinRate"
              name="Current Win Rate"
              fill="#2563eb"
              radius={[0, 4, 4, 0]}
            />

            <Bar
              dataKey="PredictedWinRate"
              name="Predicted Win Rate"
              fill="#22c55e"
              radius={[0, 4, 4, 0]}
            />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

export default IndustryWinRateChart;