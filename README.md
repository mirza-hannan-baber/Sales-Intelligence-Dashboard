# 🏢 Sales Intelligence Studio — Full-Stack AI & ML Platform

An end-to-end enterprise platform combining **Machine Learning revenue forecasting**, **interactive sales pipeline management**, and a **Groq LLaMA-powered Natural Language SQL AI Assistant**. Built with a **React 19** SPA frontend, a **.NET 9 Web API** gateway, an **EF Core / SQLite** data layer, and a **Python FastAPI ML Inference microservice**.

---

## 📐 System Architecture

```text
                                  +---------------------------------------+
                                  |         React 19 SPA Frontend         |
                                  |   (Vite, Recharts, Lucide, Axios)     |
                                  |        http://localhost:5173          |
                                  +-------------------+-------------------+
                                                      |
                                                      | HTTP / REST API
                                                      v
                                  +-------------------+-------------------+
                                  |        ASP.NET Core .NET 9 API        |
                                  |     (Identity, EF Core, Controllers)  |
                                  |        http://localhost:5082          |
                                  +---------+-------------------+---------+
                                            |                   |
                     HTTP / JSON Inference  |                   | SQL Execution
                                            v                   v
+-------------------------------------------+-----+   +---------+-------------------------+
|     Python FastAPI ML Inference Service         |   |    SQLite Database & Seeder     |
| (Random Forest, Gradient Boosting, Holt-Winters)|   |    (sales_intelligence_v3.db)   |
|            http://localhost:5001                |   +---------------------------------+
+-------------------------------------------------+             ^
                                                                | SQL Queries
                                  +-----------------------------+-----------------+
                                  |       Groq LLaMA AI SQL Agent Service        |
                                  |     (Text-to-SQL Natural Language Queries)    |
                                  +-----------------------------------------------+
```

---

## ✨ Key Platform Features

### 1. 📊 Executive Dashboard & Analytics
* Real-time Key Performance Indicators (KPIs): Total Revenue, Win Rates, Pipeline Value, Active Deals.
* Interactive visualizations built with **Recharts**: Monthly revenue trends, deal status distribution, and rep leaderboards.

### 2. 💼 Deal & Pipeline Management
* Filterable, searchable sales deal management grid.
* Sales stage tracking (Qualification, Proposal, Negotiation, Closed Won, Closed Lost).
* Deal creation modal and win-rate estimations.

### 3. 🔮 Predictive ML Intelligence
* **Company Revenue Forecasting**: Predicts future quarterly revenue using Gradient Boosting and Holt-Winters exponential smoothing models.
* **Deal Win Rate Estimator**: Evaluates deal attributes using Random Forest classification to predict closure probability.
* **Employee Performance & Revenue Analytics**: Machine Learning evaluations for sales rep quota achievement and quarterly revenue contribution.

### 4. 🤖 Groq AI Natural Language Assistant
* Context-aware sales chat drawer powered by **Groq LLaMA models**.
* Translates plain English user questions (*"Show me top 5 representatives by closed deals this quarter"*) into optimized SQLite queries.
* Executes safe `SELECT`-only queries and returns formatted data summaries.

### 5. 👥 Admin User & Access Management
* Role-based user administration (Admin / Standard User).
* Full CRUD capabilities for user management, secured by **ASP.NET Core Identity**.

---

## 🛠️ Complete Tech Stack

| Layer | Technologies Used |
| :--- | :--- |
| **Frontend** | React 19, Vite, Recharts, Lucide React, Axios, Custom Glassmorphism CSS |
| **Backend Gateway** | .NET 9 Web API, Entity Framework Core 9, ASP.NET Core Identity |
| **ML Inference Engine** | Python 3.10+, FastAPI, Uvicorn, Scikit-Learn, Pandas, NumPy, Joblib, Statsmodels |
| **AI Text-to-SQL Agent** | Groq API (LLaMA 3 / 3.3 Models), Custom SQLite Query Execution Engine |
| **Database** | SQLite 3 (`sales_intelligence_v3.db`), CSV Seeder |

---

## 📁 Repository Directory Structure

```text
Full Stack Application/
├── model-config.env                  # Single source of truth for ML dataset & model paths
├── dataset/                          # CSV datasets & trained model binaries (.pkl)
│   ├── clean_crm_data.csv
│   ├── employee_quarterly_model.pkl
│   ├── employee_yearly_model.pkl
│   ├── lag_revenue_model.pkl
│   ├── revenue_forecast_model.pkl
│   └── winrate_model.pkl
├── sales-intelligence-backend/       # .NET 9 Web API & Python ML Microservice
│   ├── Controllers/                  # REST Controllers (Auth, Deals, Predictions, Agent, Users)
│   ├── Data/                         # EF Core DbContext & Data Seeder
│   ├── Models/                       # Domain Entities & Identity Models
│   ├── Services/                     # Business logic, AI SQL Agent, Prediction Service Proxy
│   ├── ml-service/                   # Python FastAPI Inference Engine (`main.py`)
│   │   ├── main.py
│   │   └── requirements.txt
│   ├── appsettings.json              # API settings & connection strings
│   ├── Program.cs                    # Application startup & DI configuration
│   ├── README.md                     # Backend-specific documentation
│   └── sales_intelligence_v3.db     # SQLite Database
└── sales-intelligence-frontend/      # React 19 SPA Frontend
    ├── src/
    │   ├── components/               # Navbar, Sidebar, ChatWidget, Modals
    │   ├── pages/                    # Dashboard, Deals, Employees, Performance, Forecast, Users, Login
    │   ├── services/                 # Axios API HTTP Client & Endpoints
    │   └── App.jsx                   # Application routing & layout entry
    ├── package.json                  # Dependencies & npm scripts
    └── README.md                     # Frontend-specific documentation
```

---

## 🌐 Port & URL Reference Table

| Service | Host Port | Target URL | Description |
| :--- | :--- | :--- | :--- |
| **Frontend Web App** | `5173` | `http://localhost:5173` | React SPA User Interface |
| **Backend REST API** | `5082` | `http://localhost:5082/api` | ASP.NET Core Web API Gateway |
| **Backend Swagger Docs** | `5082` | `http://localhost:5082/swagger` | OpenAPI Interactive Endpoint Docs |
| **Python ML Service** | `5001` | `http://localhost:5001` | FastAPI Model Inference Server |

---

## ⚡ Quick Start Guide (Step-by-Step)

Follow these steps to launch the entire platform locally.

### Step 1: Start the Python ML Service

1. Open terminal and navigate to `sales-intelligence-backend/ml-service`:
   ```bash
   cd sales-intelligence-backend/ml-service
   ```

2. Create virtual environment and install dependencies:
   ```bash
   # Windows
   python -m venv venv
   .\venv\Scripts\activate

   # Linux/macOS
   python3 -m venv venv
   source venv/bin/activate

   # Install packages
   pip install -r requirements.txt
   ```

3. Run FastAPI server:
   ```bash
   uvicorn main:app --port 5001 --reload
   ```

---

### Step 2: Start the .NET 9 Web API Backend

1. Open a second terminal and navigate to `sales-intelligence-backend`:
   ```bash
   cd sales-intelligence-backend
   ```

2. **Create `.env` file in the `sales-intelligence-backend` folder** and paste your Groq API key(s):
   ```env
   # Primary Groq API Key required for AI Natural Language SQL Assistant
   GROQ_API_KEY=gsk_your_primary_groq_api_key_here

   # Backup Groq API Key (Optional failover key)
   GROQ_API_KEY_BACKUP=gsk_your_backup_groq_api_key_here
   ```

3. Launch .NET Web API:
   ```bash
   dotnet run
   ```
   *The API will seed the SQLite database automatically on startup if missing.*

---

### Step 3: Start the React 19 Frontend

1. Open a third terminal and navigate to `sales-intelligence-frontend`:
   ```bash
   cd sales-intelligence-frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start Vite development server:
   ```bash
   npm run dev
   ```

4. Open your browser and navigate to **`http://localhost:5173`**.

---

## 📚 Component Readme Links

Detailed documentation for each sub-project:
* 📘 [Frontend README](file:///c:/Users/Mirza/Desktop/Agile%20Tech%20Case%20Study/Full%20Stack%20Application/sales-intelligence-frontend/README.md)
* 📗 [Backend README](file:///c:/Users/Mirza/Desktop/Agile%20Tech%20Case%20Study/Full%20Stack%20Application/sales-intelligence-backend/README.md)
