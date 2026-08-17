export const FEATURE_LABELS = {
  lag_1: "Last Month's Revenue",
  lag_2: "Revenue (2 Months Ago)",
  lag_3: "Revenue (3 Months Ago)",
  lag_6: "Revenue (6 Months Ago)",
  lag_12: "Revenue (Same Month Last Year)",
  rolling_mean_3: "3-Month Average Revenue",
  rolling_std_3: "3-Month Revenue Volatility",

  // Additional camelCase or variant keys for convenience
  lag1: "Last Month's Revenue",
  lag2: "Revenue (2 Months Ago)",
  lag3: "Revenue (3 Months Ago)",
  lag6: "Revenue (6 Months Ago)",
  lag12: "Revenue (Same Month Last Year)",
  rollingMean: "3-Month Average Revenue",
  rollingMean3: "3-Month Average Revenue",
  rollingStd3: "3-Month Revenue Volatility",
};

export function getFeatureLabel(key, fallback) {
  if (!key) return fallback || '';
  return FEATURE_LABELS[key] || fallback || key;
}
