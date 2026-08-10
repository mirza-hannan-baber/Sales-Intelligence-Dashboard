import React from 'react';
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  ResponsiveContainer
} from 'recharts';

function DealRiskChart({ riskData = [] }) {
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
        Deal Risk Analysis
      </h3>

      <div
        style={{
          width: '100%',
          height: 280,
          display: 'flex',
          alignItems: 'center'
        }}
      >
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={riskData}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              innerRadius={55}
              outerRadius={80}
            >
              {riskData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} />
              ))}
            </Pie>

            <Tooltip formatter={(value) => [`${value}%`, 'Percentage']} />

            <Legend
              layout="vertical"
              align="right"
              verticalAlign="middle"
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

export default DealRiskChart;