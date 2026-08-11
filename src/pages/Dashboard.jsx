import {
  TrendingUp,
  DollarSign,
  Target,
  BrainCircuit,
} from "lucide-react";
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
} from "recharts";

import {
  revenueData,
  winRateData,
} from "../data/dashboardData";


const stats = [
  {
    title: "Total Revenue",
    value: "$2.45M",
    change: "+12.5%",
    icon: DollarSign,
  },
  {
    title: "Predicted Revenue",
    value: "$680K",
    change: "+8.4%",
    icon: TrendingUp,
  },
  {
    title: "Win Rate",
    value: "74.8%",
    change: "+4.2%",
    icon: Target,
  },
  {
    title: "AI Confidence",
    value: "92.4%",
    change: "High",
    icon: BrainCircuit,
  },
];

export default function Dashboard() {
  return (
    <div className="dashboard-page">

      <div className="page-title">

        <div>
          <h1>Dashboard</h1>

          <p>
            Welcome back, Admin. Here's your
            sales overview.
          </p>
        </div>

        <button className="primary-button">
          Generate Report
        </button>

      </div>


      {/* STATS */}
      <div className="stats-grid">

        {stats.map((stat) => {

          const Icon = stat.icon;

          return (
            <div
              className="stat-card"
              key={stat.title}
            >

              <div className="stat-card-top">

                <div className="stat-icon">
                  <Icon size={19} />
                </div>

                <span className="stat-change">
                  {stat.change}
                </span>

              </div>

              <p>{stat.title}</p>

              <h2>{stat.value}</h2>

            </div>
          );
        })}

      </div>


      {/* CHART PLACEHOLDER */}
      <div className="dashboard-chart-card">

        <div className="chart-header">

          <div>
            <h3>Revenue Overview</h3>

            <p>
              Actual and predicted revenue
            </p>
          </div>

          <select defaultValue="6">
            <option value="6">
              Last 6 Months
            </option>

            <option value="12">
              Last 12 Months
            </option>
          </select>

        </div>

        <div className="revenue-chart">

  <ResponsiveContainer
    width="100%"
    height="100%"
  >

<LineChart
  data={revenueData}
  margin={{
    top: 10,
    right: 10,
    left: 10,
    bottom: 10,
  }}
>
  <CartesianGrid strokeDasharray="3 3" />

  <XAxis
    dataKey="month"
    tick={{
      fontSize: 11,
    }}
  />

  <YAxis
    tick={{
      fontSize: 11,
    }}
    tickFormatter={(value) =>
      `$${value / 1000}K`
    }
  />

  <Tooltip
    formatter={(value, name) => [
      `$${Number(value).toLocaleString()}`,
      name === "Actual Revenue"
        ? "Actual Revenue"
        : "Predicted Revenue",
    ]}
  />

  <Line
    type="monotone"
    dataKey="actualRevenue"
    name="Actual Revenue"
    stroke="#4f46e5"
    strokeWidth={3}
    dot={{ r: 4 }}
    activeDot={{ r: 7 }}
  />

  <Line
    type="monotone"
    dataKey="predictedRevenue"
    name="Predicted Revenue"
    stroke="#94a3b8"
    strokeWidth={3}
    strokeDasharray="6 5"
    dot={{ r: 4 }}
    activeDot={{ r: 7 }}
  />

</LineChart>

  </ResponsiveContainer>

</div>

</div>
<div className="dashboard-bottom-grid">

  {/* WIN RATE CHART */}
  <div className="dashboard-small-card">

    <div className="small-card-header">

      <div>
        <h3>Win Rate Trend</h3>

        <p>
          Monthly sales win rate
        </p>
      </div>

    </div>


    <div className="win-rate-chart">

      <ResponsiveContainer
        width="100%"
        height="100%"
      >

        <LineChart
          data={winRateData}
          margin={{
            top: 10,
            right: 10,
            left: 0,
            bottom: 5,
          }}
        >

          <CartesianGrid
            strokeDasharray="3 3"
          />

          <XAxis
            dataKey="month"
            tick={{
              fontSize: 11,
            }}
          />

          <YAxis
            domain={[50, 100]}
            tick={{
              fontSize: 11,
            }}
            tickFormatter={(value) =>
              `${value}%`
            }
          />

          <Tooltip
            formatter={(value) => [
              `${value}%`,
              "Win Rate",
            ]}
          />

          <Line
            type="monotone"
            dataKey="winRate"
            name="Win Rate"
            stroke="#4f46e5"
            strokeWidth={3}
            dot={{ r: 4 }}
            activeDot={{ r: 7 }}
          />

        </LineChart>

      </ResponsiveContainer>

    </div>

  </div>


  {/* SECOND CARD */}
  <div className="dashboard-small-card">

    <div className="small-card-header">

      <div>
        <h3>Sales Performance</h3>

        <p>
          Current performance overview
        </p>
      </div>

    </div>

    <div className="performance-placeholder">
      Employee Performance
    </div>

  </div>

</div>
    </div>
  );
}