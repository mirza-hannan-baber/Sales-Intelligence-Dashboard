# ⚙️ Sales Intelligence Studio - Backend API & ML Microservice

The core server-side architecture for the **Sales Intelligence Studio** platform, built with **.NET 9 Web API** and an integrated **Python FastAPI Machine Learning Microservice**.

---

## 🎯 Architectural Overview

The backend is structured into two primary services:

1. **ASP.NET Core Web API (.NET 9)**:
   * Provides RESTful HTTP endpoints for authentication, deals, dashboard KPIs, predictions, and database query execution.
   * Leverages **Entity Framework Core** with **SQLite** (`sales_intelligence_v3.db`) for relational data management.
   * Integrates **ASP.NET Core Identity** for secure password hashing and user management.
   * Features **Groq LLaMA AI SQL Agent** (`GroqAgentService`) for natural language text-to-SQL query generation.

2. **Python ML Inference Microservice (`ml-service`)**:
   * Built using **FastAPI** & **Uvicorn**.
   * Serves trained machine learning models (Gradient Boosting, Random Forest, Holt-Winters exponential smoothing) for revenue forecasting, win-rate estimation, and sales rep performance scoring.
   * Reads central configuration from `model-config.env` at the workspace root.

---

## 🛠️ Technology Stack

* **Core Backend Framework**: .NET 9 Web API
* **ORM**: Entity Framework Core 9 (SQLite Provider)
* **Authentication**: ASP.NET Core Identity
* **API Documentation**: OpenAPI / Swagger UI (`/swagger`)
* **ML Microservice**: Python 3.10+, FastAPI, Uvicorn, Scikit-Learn, Pandas, NumPy, Joblib, Statsmodels
* **AI Provider**: Groq API (LLaMA 3 / LLaMA 3.3 models)

---

## 📂 Backend Project Structure

```text
sales-intelligence-backend/
├── Controllers/                 # REST API Controllers
│   ├── AuthController.cs        # User authentication & registration
│   ├── DashboardController.cs   # KPI dashboard aggregates
│   ├── DealsController.cs       # Deal pipeline management
│   ├── PredictionsController.cs # ML service proxy & logging
│   ├── UsersController.cs       # Admin user management
│   └── AgentController.cs       # AI SQL agent endpoint
├── Data/                        # EF Core DbContext & Seeder
│   ├── ApplicationDbContext.cs  # SQLite DbContext definition
│   └── DbInitializer.cs         # Seed logic from CSV dataset
├── DTOs/                        # Request/Response Data Transfer Objects
├── Models/                      # EF Core Entities & Identity models
├── Services/                    # Core Business Logic & AI/ML Integrations
│   ├── GroqAgentService.cs      # Groq LLaMA Text-to-SQL logic
│   ├── PredictionService.cs     # Python ML HTTP client proxy
│   ├── ForecastService.cs       # Forecasting calculations
│   └── SqlQueryExecutor.cs      # Dynamic SQLite execution & safety checks
├── ml-service/                  # Python FastAPI ML Service
│   ├── main.py                  # FastAPI app & model inference handlers
│   └── requirements.txt         # Python dependencies
├── appsettings.json             # API Configuration & Connection strings
├── Program.cs                   # Application entry point & dependency injection
└── sales_intelligence_v3.db    # SQLite Database file
```

---

## 🔑 Environment Configuration

Create a `.env` file in the `sales-intelligence-backend` directory (or workspace root) to configure your Groq API key(s) for the AI Sales Assistant.

### Creating `sales-intelligence-backend/.env`

Create `sales-intelligence-backend/.env` and paste your Groq API key(s):

```env
# Primary Groq API Key required for AI Natural Language SQL Assistant
GROQ_API_KEY=gsk_your_primary_groq_api_key_here

# Backup Groq API Key (Optional failover key if primary hits rate limits)
GROQ_API_KEY_BACKUP=gsk_your_backup_groq_api_key_here
```

| Variable Name | Description | Required |
| :--- | :--- | :--- |
| `GROQ_API_KEY` | Primary API Key from [Groq Console](https://console.groq.com/) for LLaMA 3.3 Text-to-SQL | Yes (for AI Chat) |
| `GROQ_API_KEY_BACKUP` | Secondary/Backup Groq API Key used if primary reaches rate limits | Optional |

### `appsettings.json` Settings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=sales_intelligence_v3.db"
  },
  "MlService": {
    "BaseUrl": "http://localhost:5001"
  }
}
```

---

## 🚀 Getting Started

### Prerequisites
* **.NET 9 SDK**: Version 9.0 or higher
* **Python**: 3.10 or higher
* **Groq API Key**: Required for AI Chat functionality

---

### Step 1: Start the Python ML Microservice

1. Navigate to the `ml-service` directory:
   ```bash
   cd sales-intelligence-backend/ml-service
   ```

2. Create and activate a Python virtual environment:
   ```bash
   # Windows
   python -m venv venv
   .\venv\Scripts\activate

   # Linux/macOS
   python3 -m venv venv
   source venv/bin/activate
   ```

3. Install required Python packages:
   ```bash
   pip install -r requirements.txt
   ```

4. Launch the FastAPI server:
   ```bash
   uvicorn main:app --port 5001 --reload
   ```
   *ML Service will be running at `http://localhost:5001`.*

---

### Step 2: Start the .NET Web API

1. Navigate back to `sales-intelligence-backend`:
   ```bash
   cd sales-intelligence-backend
   ```

2. Create `.env` file in `sales-intelligence-backend` folder and add your Groq API key(s):
   ```env
   GROQ_API_KEY=gsk_your_primary_groq_api_key_here
   GROQ_API_KEY_BACKUP=gsk_your_backup_groq_api_key_here
   ```

3. Restore .NET dependencies:
   ```bash
   dotnet restore
   ```

4. Run the API:
   ```bash
   dotnet run
   ```
   *The API will start at `http://localhost:5082` (or configured port).*  
   *Swagger UI documentation is available at `http://localhost:5082/swagger`.*

---

## 📊 ML Microservice Endpoints

| Endpoint | Method | Description |
| :--- | :--- | :--- |
| `/health` | GET | Check service health & loaded ML models status |
| `/predict/company-revenue` | POST | Predict company-level revenue with lag feature inputs |
| `/predict/win-rate` | POST | Estimate deal win probability based on deal attributes |
| `/predict/employee-performance`| POST | Calculate sales rep quarterly achievement probability |
| `/predict/employee-revenue` | POST | Forecast individual sales rep revenue contribution |

---

## 🛡️ Security & Query Execution Safety

* **Database Queries**: All dynamic queries generated by the AI agent pass through `SqlQueryExecutor.cs` which enforces `SELECT`-only execution restrictions to prevent SQL injection and unauthorized data modification (`INSERT`/`UPDATE`/`DELETE`).
* **Authentication**: Password verification and token storage are secured via ASP.NET Core Identity.
