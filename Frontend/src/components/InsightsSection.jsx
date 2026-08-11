import React from 'react';
import {
  Lightbulb,
  ShoppingCart,
  HeartPulse,
  Users,
  Award
} from 'lucide-react';
import InsightCard from './InsightCard';

function InsightsSection() {
  return (
    <div>
      <h3
        style={{
          fontSize: '16px',
          fontWeight: 'bold',
          marginBottom: '16px',
          color: '#0f172a'
        }}
      >
        AI Insights & Recommendations
      </h3>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '14px'
        }}
      >
        <InsightCard
          icon={Lightbulb}
          bgColor="#eff6ff"
          iconColor="#2563eb"
          text="Revenue is expected to increase by 7.4% in the next quarter based on current pipeline trends."
        />

        <InsightCard
          icon={ShoppingCart}
          bgColor="#f0fdf4"
          iconColor="#16a34a"
          text="Retail industry shows the highest predicted growth in win rate (71%). Focus on this sector."
        />

        <InsightCard
          icon={HeartPulse}
          bgColor="#faf5ff"
          iconColor="#9333ea"
          text="Healthcare industry win rate is expected to decline by 3.2%. Re-evaluate strategy and offerings."
        />

        <InsightCard
          icon={Users}
          bgColor="#fff7ed"
          iconColor="#ea580c"
          text="Deals with longer sales cycle (> 60 days) have 23% lower win probability. Accelerate follow-ups."
        />

        <InsightCard
          icon={Award}
          bgColor="#ecfeff"
          iconColor="#0891b2"
          text="Focus on large deal size opportunities. They have 38% higher chance of winning."
        />
      </div>
    </div>
  );
}

export default InsightsSection;