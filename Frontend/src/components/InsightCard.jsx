import React from 'react';

function InsightCard({ icon: Icon, bgColor, iconColor, text }) {
  return (
    <div
      style={{
        background: bgColor,
        padding: '16px',
        borderRadius: '12px',
        border: '1px solid #e2e8f0',
        display: 'flex',
        flexDirection: 'column',
        gap: '12px'
      }}
    >
      <div
        style={{
          background: '#fff',
          width: '36px',
          height: '36px',
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center'
        }}
      >
        {Icon && <Icon size={20} color={iconColor} />}
      </div>
      <p
        style={{
          fontSize: '12px',
          color: '#334155',
          margin: 0,
          lineHeight: '1.5'
        }}
      >
        {text}
      </p>
    </div>
  );
}

export default InsightCard;