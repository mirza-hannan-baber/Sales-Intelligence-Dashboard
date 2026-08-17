# 📊 Sales Intelligence Studio - Frontend Application

A modern, high-performance **React 19** web application built with **Vite**, **Recharts**, and **Lucide React**. It provides executive dashboards, interactive sales deal pipelines, ML-driven revenue and win-rate prediction interfaces, sales rep performance analytics, and an integrated **AI Natural Language Sales Assistant**.

---

## 🚀 Key Features

* **📈 Executive Dashboard**: Key performance indicators (KPIs), revenue metrics, interactive trend charts, and real-time deal stats.
* **💼 Deals Pipeline**: Filterable and searchable deal management grid, stage tracking, win rate calculations, and deal creation modal.
* **👥 Sales Force Management**: Representative directory, individual performance tracking, and sales quota achievements.
* **🔮 Predictive Intelligence UI**:
  * **Company Revenue Forecast**: ML-backed quarterly revenue projections with interactive parameters.
  * **Employee Revenue Predictor**: Forecast rep-specific sales contributions.
  * **Employee Performance Evaluator**: ML evaluation scoring and quota achievement probability.
* **🤖 Groq AI Sales Assistant**: Embedded natural language chat drawer to query database insights in plain English.
* **🔐 Admin User Management**: Comprehensive user access controls and role management (Admin/User).
* **🎨 Glassmorphism Dark UI**: Modern, responsive user interface styled with custom CSS tokens, smooth animations, and zero heavy UI framework overhead.

---

## 🛠️ Technology Stack

| Category | Technology |
| :--- | :--- |
| **Framework** | [React 19](https://react.dev/) |
| **Build Tool & Dev Server** | [Vite 6](https://vitejs.dev/) |
| **Routing** | [React Router DOM v7](https://reactrouter.com/) |
| **Data Visualization** | [Recharts 3](https://recharts.org/) |
| **Icon System** | [Lucide React](https://lucide.dev/) |
| **HTTP Client** | [Axios](https://axios-http.com/) |
| **Styling** | Custom Vanilla CSS (Design Tokens, Responsive Grid, Dark Mode) |

---

## 📁 Project Structure

```text
sales-intelligence-frontend/
├── public/                  # Static assets
├── src/
│   ├── assets/              # Images, SVGs, static files
│   ├── components/          # Reusable UI components
│   │   ├── Navbar.jsx       # Header bar with user profile & status
│   │   ├── Sidebar.jsx      # Navigation sidebar with route links
│   │   └── ChatWidget.jsx   # AI assistant drawer component
│   ├── context/             # Global Context Providers
│   │   └── AuthContext.jsx  # Authentication state & session management
│   ├── hooks/               # Custom React hooks
│   ├── layouts/             # Page layout wrappers
│   │   └── MainLayout.jsx   # Main application shell layout
│   ├── pages/               # Top-level application views
│   │   ├── Dashboard.jsx            # KPI & Executive summary
│   │   ├── Deals.jsx                # Deal pipeline & management
│   │   ├── Employees.jsx            # Sales representative overview
│   │   ├── EmployeePerformance.jsx  # ML performance analysis
│   │   ├── EmployeeRevenue.jsx      # Rep revenue predictions
│   │   ├── RevenueForecast.jsx      # Enterprise revenue predictions
│   │   ├── Users.jsx                # Admin user management
│   │   └── Login.jsx                # User login & authentication
│   ├── services/            # API integration layer
│   │   └── api.js           # Axios instance & service endpoints
│   ├── utils/               # Formatting & helper functions
│   ├── App.jsx              # Main App component & routes
│   ├── index.css            # Global CSS design system & utilities
│   └── main.jsx             # React DOM entry point
├── .env.example             # Environment variable template
├── eslint.config.js         # ESLint configuration
├── package.json             # Dependencies & npm scripts
└── vite.config.js           # Vite configuration
```

---

## ⚙️ Environment Configuration

Create a `.env` file in the `sales-intelligence-frontend` root directory based on `.env.example`:

```env
VITE_API_BASE_URL=http://localhost:5082/api
```

| Variable | Description | Default |
| :--- | :--- | :--- |
| `VITE_API_BASE_URL` | Base URL of the backend ASP.NET Core API | `http://localhost:5082/api` |

---

## 🚀 Getting Started

### Prerequisites
* **Node.js**: v18.0.0 or higher
* **npm**: v9.0.0 or higher

### Installation & Setup

1. **Navigate to the frontend directory**:
   ```bash
   cd sales-intelligence-frontend
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Configure Environment Variables**:
   Copy `.env.example` to `.env` if custom backend URL is required:
   ```bash
   cp .env.example .env
   ```

4. **Run Development Server**:
   ```bash
   npm run dev
   ```
   The application will start at `http://localhost:5173`.

---

## 📜 Available Scripts

In the `sales-intelligence-frontend` directory, you can run:

* `npm run dev`: Launches Vite development server with HMR (Hot Module Replacement).
* `npm run build`: Compiles and bundles optimized production assets into `dist/`.
* `npm run preview`: Locally previews the production build.
* `npm run lint`: Executes ESLint code quality checks.

---

## 🔌 API Integration Overview

The frontend communicates with the backend via `src/services/api.js`. Key services include:

* `authService`: Handles login, logout, and localStorage session management.
* `dashboardService`: Fetches KPI summary metrics.
* `dealsService`: Retrieves, creates, and inspects deal entities.
* `predictionsService`: Connects to backend endpoints for ML model predictions.
* `agentService` / `chatService`: Sends natural language questions to the Groq SQL Agent.
* `userService`: Handles Admin CRUD operations for application users.

Response interceptors automatically manage `401 Unauthorized` responses by redirecting to `/login`.
