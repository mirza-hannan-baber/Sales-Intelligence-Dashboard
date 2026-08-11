import { useMemo, useState } from "react";

import {
  BarChart,
  Bar,
  CartesianGrid,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

import { employeePerformanceData } from "../data/employeePerformanceData";

function EmployeePerformance() {
const [searchTerm, setSearchTerm] = useState("");
const [department, setDepartment] = useState("All");
const [period, setPeriod] = useState("Last 6 Months");
const filteredEmployees = useMemo(() => {
  return employeePerformanceData.filter((employee) => {
    const matchesSearch = employee.name
      .toLowerCase()
      .includes(searchTerm.toLowerCase());

    const matchesDepartment =
      department === "All" ||
      employee.department === department;

    return matchesSearch && matchesDepartment;
  });
}, [searchTerm, department]);
const totalEmployees = filteredEmployees.length;

const averageRevenue =
  totalEmployees > 0
    ? filteredEmployees.reduce(
        (sum, employee) => sum + employee.revenue,
        0
      ) / totalEmployees
    : 0;

const averageWinRate =
  totalEmployees > 0
    ? filteredEmployees.reduce(
        (sum, employee) => sum + employee.winRate,
        0
      ) / totalEmployees
    : 0;

const topPerformer =
  filteredEmployees.length > 0
    ? [...filteredEmployees].sort(
        (a, b) =>
          b.performanceScore - a.performanceScore
      )[0]
    : null;

    const chartData = filteredEmployees.map((employee) => ({
  name: employee.name,
  performanceScore: employee.performanceScore,
}));
return (
  <div className="employee-performance-page">

    <div className="page-header">

      <div>
        <h1>Employee Performance</h1>

        <p>
          Track and analyze your sales team's performance
        </p>
      </div>

    </div>
    <div className="performance-filters">

  <div className="search-box">

    <input
      type="text"
      placeholder="Search employee..."
      value={searchTerm}
      onChange={(e) =>
        setSearchTerm(e.target.value)
      }
    />

  </div>


  <select
    value={department}
    onChange={(e) =>
      setDepartment(e.target.value)
    }
  >
    <option value="All">
      All Departments
    </option>

    <option value="Sales">
      Sales
    </option>

    <option value="Business Development">
      Business Development
    </option>
  </select>


  <select
    value={period}
    onChange={(e) =>
      setPeriod(e.target.value)
    }
  >
    <option> This Month </option>
    <option> Last Month </option>
    <option> Last 3 Months </option>
    <option> Last 6 Months </option>
    <option> This Year </option>
  </select>

</div>
<div className="performance-kpi-grid">

  <div className="performance-kpi-card">
    <span>Total Employees</span>

    <strong>
      {totalEmployees}
    </strong>

    <small>
      Active employees
    </small>
  </div>


  <div className="performance-kpi-card">
    <span>Average Revenue</span>

    <strong>
      ${Math.round(averageRevenue).toLocaleString()}
    </strong>

    <small>
      Per employee
    </small>
  </div>


  <div className="performance-kpi-card">
    <span>Average Win Rate</span>

    <strong>
      {averageWinRate.toFixed(1)}%
    </strong>

    <small>
      Across employees
    </small>
  </div>


  <div className="performance-kpi-card">
    <span>Top Performer</span>

    <strong className="top-performer-name">
      {topPerformer
        ? topPerformer.name
        : "N/A"}
    </strong>

    <small>
      {topPerformer
        ? `${topPerformer.performanceScore} performance score`
        : "No data"}
    </small>
  </div>

</div>
<div className="performance-chart-card">

  <div className="section-heading">

    <div>
      <h2>Performance Comparison</h2>

      <p>
        Employee performance score
      </p>
    </div>

  </div>


  <div className="performance-chart">

    <ResponsiveContainer
      width="100%"
      height="100%"
    >

      <BarChart
        data={chartData}
        margin={{
          top: 10,
          right: 20,
          left: 0,
          bottom: 10,
        }}
      >

        <CartesianGrid
          strokeDasharray="3 3"
        />

        <XAxis
          dataKey="name"
          tick={{
            fontSize: 10,
          }}
        />

        <YAxis
          domain={[0, 100]}
          tick={{
            fontSize: 11,
          }}
        />

        <Tooltip
          formatter={(value) => [
            value,
            "Performance Score",
          ]}
        />

        <Bar
          dataKey="performanceScore"
          name="Performance Score"
          fill="#4f46e5"
          radius={[6, 6, 0, 0]}
        />

      </BarChart>

    </ResponsiveContainer>

  </div>

</div>
<div className="employee-table-card">

  <div className="section-heading">

    <div>
      <h2>Employee Performance Details</h2>

      <p>
        Detailed performance overview
      </p>
    </div>

  </div>


  <div className="employee-table-wrapper">

    <table>

      <thead>

        <tr>
          <th>Employee</th>
          <th>Revenue</th>
          <th>Deals</th>
          <th>Won</th>
          <th>Win Rate</th>
          <th>Performance</th>
          <th>Status</th>
        </tr>

      </thead>


      <tbody>

        {filteredEmployees.map((employee) => (

          <tr key={employee.id}>

            <td>
              <div className="employee-name">

                <strong>
                  {employee.name}
                </strong>

                <span>
                  {employee.role}
                </span>

              </div>
            </td>


            <td>
              ${employee.revenue.toLocaleString()}
            </td>


            <td>
              {employee.totalDeals}
            </td>


            <td>
              {employee.wonDeals}
            </td>


            <td>
              {employee.winRate}%
            </td>


            <td>

              <div className="score-cell">

                <span>
                  {employee.performanceScore}
                </span>

                <div className="score-bar">

                  <div
                    style={{
                      width: `${employee.performanceScore}%`,
                    }}
                  />

                </div>

              </div>

            </td>


            <td>

              <span
                className={`status-badge status-${employee.status
                  .toLowerCase()
                  .replaceAll(" ", "-")}`}
              >
                {employee.status}
              </span>

            </td>

          </tr>

        ))}

      </tbody>

    </table>

  </div>

</div>  
</div>
);
}

export default EmployeePerformance;
