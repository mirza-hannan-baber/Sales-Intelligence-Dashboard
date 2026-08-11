import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  CartesianGrid
} from 'recharts';

function FeatureImportanceChart({ featureImportance = [] }) {
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
        Top Factors Affecting Win Rate
      </h3>

      <div
        style={{
          width: '100%',
          height: 280
        }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={featureImportance} layout="vertical">
            <CartesianGrid strokeDasharray="3 3" horizontal={false} />

            <XAxis type="number" unit="%" domain={[0, 50]} />

            <YAxis
              dataKey="factor"
              type="category"
              width={110}
              tick={{ fontSize: 12, fill: '#64748b' }}
            />

            <Tooltip formatter={(value) => [`${value}%`, 'Importance']} />

            <Bar
              dataKey="importance"
              name="Importance %"
              fill="#8b5cf6"
              radius={[0, 4, 4, 0]}
            />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

export default FeatureImportanceChart;