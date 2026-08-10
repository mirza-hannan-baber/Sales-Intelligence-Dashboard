import React from 'react';

function KpiBox({ icon: Icon, label, val, change, isPos }) {
  return (
    <div
      style={{
        background: '#fff',
        padding: '16px',
        borderRadius: '12px',
        border: '1px solid #e2e8f0'
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          marginBottom: '8px'
        }}
      >
        {Icon && <Icon size={18} color="#2563eb" />}
        <span style={{ fontSize: '11px', color: '#64748b' }}>{label}</span>
      </div>
      <h2
        style={{
          fontSize: '20px',
          fontWeight: 'bold',
          margin: '4px 0',
          color: '#0f172a'
        }}
      >
        {val}
      </h2>
      <span style={{ fontSize: '11px', color: isPos ? '#16a34a' : '#dc2626' }}>
        {change}
      </span>
    </div>
  );
}

export default KpiBox;