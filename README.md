# Sales Intelligence Dashboard - Frontend

## Project Overview

Sales Intelligence Dashboard is a web-based analytics application designed to provide sales insights, employee performance analysis, revenue forecasting, and deal-related predictions.

This repository contains the **React frontend/UI** of the project.

At the current stage, the frontend is being developed independently using mock/static data. The backend, database, APIs, and ML model integration will be connected in the next development phase.

---

## Current Project Status

### Frontend Development

The following frontend functionality has been implemented:

- Admin Dashboard UI
- Responsive sidebar navigation
- Dashboard KPI cards
- Revenue visualization
- Actual vs Predicted Revenue chart
- Interactive charts with tooltips
- Employee Performance analytics page
- Employee Performance KPI cards
- Employee Performance comparison chart
- Employee Performance details table
- Employee search and filtering UI
- Employee performance status indicators
- React Router based page navigation
- Admin layout structure
- Reusable frontend components
- Mock data structure for frontend development

---

## Employee Performance Module

The Employee Performance module provides an overview of individual employee performance.

It currently includes:

- Total number of employees
- Average employee revenue
- Average win rate
- Top performing employee
- Employee performance comparison
- Employee revenue
- Total deals
- Won deals
- Lost deals
- Win rate
- Performance score
- Employee performance status

The current module uses mock data for UI development.

This data will later be replaced with real data retrieved from the backend API.

---

## Frontend Technology Stack

- React
- JavaScript
- Vite
- React Router
- Recharts
- CSS
- Axios/API service structure

---

## Current Architecture

The current development architecture is:

React Frontend
↓
Mock / Static Data

The backend and database are not connected yet.

---

## Planned Backend Architecture

The planned architecture is:

React Frontend
↓
ASP.NET Core Web API
↓
SQL Server Database

The ASP.NET Core backend will handle:

- Authentication
- Authorization
- Role-based access
- Employee management
- Deal management
- User management
- Dashboard data
- Analytics data
- API communication

---

## Planned ML Integration

The project will later integrate trained machine learning models for analytics and prediction.

Planned ML functionality includes:

### Revenue Forecasting

Predict future revenue using historical sales/revenue data.

### Employee Performance Analytics

Analyze employee performance using sales activity, revenue, deals, win rate, and other relevant features.

### Deal Win Probability

Predict the probability of winning an individual sales deal.

The ML models will be integrated through a backend/ML service rather than being executed directly inside the React frontend.

---

## Planned Authentication & Authorization

The application will include role-based authentication and authorization.

Planned roles include:

- SuperAdmin
- Admin
- Employee

SuperAdmin/Admin users will be responsible for managing users and controlling access to appropriate functionality.

---

## Development Roadmap

### Phase 1 - Frontend UI

- [x] Admin Dashboard
- [x] Sidebar Navigation
- [x] Employee Performance UI
- [x] Dashboard charts
- [x] KPI cards
- [x] Mock data
- [ ] Revenue Forecast UI
- [ ] Employee Revenue UI
- [ ] Deal Probability UI
- [ ] Deals Management UI
- [ ] Employee Management UI
- [ ] User Management UI
- [ ] Profile & Settings

### Phase 2 - Backend

- [ ] ASP.NET Core Web API
- [ ] SQL Server Database
- [ ] Entity Models
- [ ] Entity Framework Core
- [ ] Database Migrations
- [ ] REST APIs
- [ ] Authentication
- [ ] JWT
- [ ] Role-based Authorization

### Phase 3 - Frontend & Backend Integration

- [ ] Connect React with ASP.NET Core APIs
- [ ] Replace mock data with API data
- [ ] Implement API error handling
- [ ] Implement loading states
- [ ] Implement authenticated requests

### Phase 4 - Machine Learning Integration

- [ ] Connect Revenue Forecast model
- [ ] Connect Employee Performance model
- [ ] Connect Deal Win Probability model
- [ ] Create ML prediction APIs
- [ ] Integrate predictions into frontend

---

## Current Status

**Frontend UI Development - In Progress**

The current repository represents the frontend development stage of the Sales Intelligence Dashboard.

Backend, database, authentication, API integration, and machine learning integration will be implemented in subsequent phases.
