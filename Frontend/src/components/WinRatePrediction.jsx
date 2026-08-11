import React from 'react';

function WinRatePrediction({ currentWinRate = '0', predictedWinRate = '0' }) {
  const current = Number(currentWinRate) || 0;
  const predicted = Number(predictedWinRate) || 0;
  const improvement = (predicted - current).toFixed(1);

  return (
    <div
      style={{
        background: '#fff',
        padding: '20px',
        borderRadius: '12px',
        border: '1px solid #e2e8f0',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100%',
        boxSizing: 'border-box'
      }}
    >
      <h3
        style={{
          fontSize: '15px',
          fontWeight: 'bold',
          marginTop: 0,
          marginBottom: '16px',
          color: '#1e293b'
        }}
      >
        Win Rate Prediction
      </h3>

      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '20px',
          width: '100%',
          justifyContent: 'center'
        }}
      >
        <div
          style={{
            textAlign: 'center',
            borderRadius: '8px',
            padding: '8px 12px',
            backgroundColor: '#f1f5f9',
            border: '1px solid #cbd5e1'
          }}
        >
          <div style={{ fontSize: '12px', color: '#64748b' }}>
            Current Win Rate
          </div>
          <div
            style={{
              fontSize: '24px',
              fontWeight: 'bold',
              color: '#2563eb'
            }}
          >
            {currentWinRate}%
          </div>
        </div>

        <div style={{ fontSize: '20px', color: '#64748b' }}>→</div>

        <div
          style={{
            textAlign: 'center',
            borderRadius: '8px',
            padding: '8px 12px',
            backgroundColor: '#f1f5f9',
            border: '1px solid #cbd5e1'
          }}
        >
          <div style={{ fontSize: '12px', color: '#64748b' }}>
            Predicted Next Quarter
          </div>
          <div
            style={{
              fontSize: '24px',
              fontWeight: 'bold',
              color: '#16a34a'
            }}
          >
            {predictedWinRate}%
          </div>
        </div>
      </div>

      <div
        style={{
          marginTop: '16px',
          fontSize: '13px',
          color: Number(improvement) >= 0 ? '#16a34a' : '#dc2626',
          fontWeight: 'bold'
        }}
      >
        {Number(improvement) >= 0 ? '▲' : '▼'} {Math.abs(Number(improvement))}% Improvement
      </div>
    </div>
  );
}

export default WinRatePrediction;