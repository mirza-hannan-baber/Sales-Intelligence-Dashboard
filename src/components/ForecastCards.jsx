import React from 'react';

function ForecastCards({ forecastCards = [] }) {
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
          marginBottom: '16px',
          color: '#1e293b',
          textAlign: 'center'
        }}
      >
        Next 3 Months Revenue Forecast
      </h3>

      <div style={{ display: 'flex', gap: '10px' }}>
        {forecastCards.map((fc, idx) => {
          const prevRevenue =
            idx > 0 ? Number(forecastCards[idx - 1].predicted_revenue) : null;

          const currentRevenue = Number(fc.predicted_revenue || 0);

          const isIncreasing =
            prevRevenue !== null ? currentRevenue >= prevRevenue : true;

          const trendColor = isIncreasing ? '#16a34a' : '#dc2626';
          const trendIcon = isIncreasing ? '▲' : '▼';

          const pathData = isIncreasing
            ? 'M0,25 L15,20 L30,22 L45,12 L60,15 L75,5 L90,8 L100,2'
            : 'M0,2 L15,8 L30,5 L45,15 L60,12 L75,22 L90,20 L100,25';

          return (
            <div
              key={idx}
              style={{
                flex: 1,
                padding: '14px 12px 8px 12px',
                background: '#f8fafc',
                borderRadius: '8px',
                border: '1px solid #cbd5e1',
                textAlign: 'center',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between',
                overflow: 'hidden'
              }}
            >
              <div>
                <div
                  style={{
                    fontSize: '12px',
                    color: '#202020',
                    fontWeight: 'bold'
                  }}
                >
                  {idx === 0 ? 'Next Month' : `Month ${idx + 1}`}
                </div>

                <div
                  style={{
                    fontSize: '12px',
                    color: '#64748b',
                    fontWeight: 'bold'
                  }}
                >
                  {fc.month_label}
                </div>

                <div
                  style={{
                    fontSize: '18px',
                    fontWeight: 'bold',
                    color: '#0f172a',
                    margin: '8px 0'
                  }}
                >
                  ${(currentRevenue / 1000).toFixed(2)}K
                </div>

                <div
                  style={{
                    fontSize: '11px',
                    color: trendColor,
                    fontWeight: '600',
                    marginTop: '12px'
                  }}
                >
                  {trendIcon} {fc.revenue_confidence_pct}% Conf
                </div>
              </div>

              <div
                style={{
                  marginTop: '10px',
                  height: '30px',
                  width: '100%'
                }}
              >
                <svg
                  viewBox="0 0 100 30"
                  width="100%"
                  height="100%"
                  preserveAspectRatio="none"
                >
                  <path
                    d={pathData}
                    fill="none"
                    stroke={trendColor}
                    strokeWidth="2.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default ForecastCards;