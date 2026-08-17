"""
Sales Intelligence ML inference service (FastAPI).

Loads the migrated model set described in the workspace-root `model-config.env`
file. Every model is loaded defensively: a failure to load one artifact never
prevents the service from starting, and `/health` reports exactly which models
are available. Model file locations are read from the central config so the
dataset/model set can be swapped without touching this code.
"""
import os
import math
import logging
from typing import Any, Dict, List, Optional

import numpy as np
import pandas as pd
import joblib
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel


# ---------------------------------------------------------------------------
# Central configuration loading (model-config.env at the workspace root)
# ---------------------------------------------------------------------------
def _find_config_file(start_dir: str, filename: str = "model-config.env") -> Optional[str]:
    """Walk up from start_dir looking for the central config file."""
    current = os.path.abspath(start_dir)
    while True:
        candidate = os.path.join(current, filename)
        if os.path.isfile(candidate):
            return candidate
        parent = os.path.dirname(current)
        if parent == current:
            return None
        current = parent


def _parse_env_file(path: str) -> Dict[str, str]:
    values: Dict[str, str] = {}
    with open(path, "r", encoding="utf-8") as fh:
        for raw in fh:
            line = raw.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            key, _, val = line.partition("=")
            values[key.strip()] = val.strip().strip('"').strip("'")
    return values


_CONFIG_PATH = _find_config_file(os.path.dirname(__file__))
if _CONFIG_PATH is None:
    # Fall back to a best guess so the service can still boot and report status.
    _WORKSPACE_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    CONFIG: Dict[str, str] = {}
    print("[ml-service] WARNING: model-config.env not found; using defaults.")
else:
    _WORKSPACE_ROOT = os.path.dirname(_CONFIG_PATH)
    CONFIG = _parse_env_file(_CONFIG_PATH)
    print(f"[ml-service] Loaded config from {_CONFIG_PATH}")


def _cfg(key: str, default: str) -> str:
    return CONFIG.get(key, default)


MODEL_DIR = os.path.join(_WORKSPACE_ROOT, _cfg("MODEL_DIR", "dataset"))
DATASET_PATH = os.path.join(_WORKSPACE_ROOT, _cfg("DATASET_PATH", "dataset/clean_crm_data.csv"))


def load_pkl(config_key: str, default_filename: str):
    """Safely load a joblib/pickle artifact named in the config. Returns None on failure."""
    filename = _cfg(config_key, default_filename)
    filepath = os.path.join(MODEL_DIR, filename)
    if not os.path.exists(filepath):
        print(f"[ml-service] MISSING artifact: {filepath}")
        return None
    try:
        obj = joblib.load(filepath)
        print(f"[ml-service] Loaded {filename}")
        return obj
    except Exception as exc:  # noqa: BLE001 - never let one bad artifact crash boot
        print(f"[ml-service] ERROR loading {filename}: {exc}")
        return None


# ---------------------------------------------------------------------------
# Model artifacts
# ---------------------------------------------------------------------------
lag_revenue_model = load_pkl("LAG_REVENUE_MODEL", "lag_revenue_model.pkl")
lag_revenue_features = load_pkl("LAG_REVENUE_FEATURES", "lag_revenue_features.pkl") or [
    "lag_1", "lag_2", "lag_3", "lag_6", "lag_12", "rolling_mean_3", "rolling_std_3", "month", "quarter",
]
revenue_forecast_model = load_pkl("REVENUE_FORECAST_MODEL", "revenue_forecast_model.pkl")
winrate_model = load_pkl("WINRATE_MODEL", "winrate_model.pkl")
winrate_columns = load_pkl("WINRATE_MODEL_COLUMNS", "winrate_model_columns.pkl") or {}
employee_quarterly_model = load_pkl("EMPLOYEE_QUARTERLY_MODEL", "employee_quarterly_model.pkl")
employee_quarterly_features = load_pkl("EMPLOYEE_QUARTERLY_FEATURES", "employee_quarterly_features.pkl") or [
    "deals_worked", "win_rate", "avg_deal_size", "avg_cycle_days", "revenue",
]
employee_yearly_model = load_pkl("EMPLOYEE_YEARLY_MODEL", "employee_yearly_model.pkl")
employee_yearly_features = load_pkl("EMPLOYEE_YEARLY_FEATURES", "employee_yearly_features.pkl") or [
    "revenue", "years_active", "deals_worked", "win_rate",
]

# Precomputed 3-month revenue forecast (robust fallback for Holt-Winters output).
_FORECAST_CSV = os.path.join(
    MODEL_DIR,
    _cfg("REVENUE_FORECAST_FALLBACK", "revenue_forecast_next_3_months.csv"),
)


app = FastAPI(title="Sales Intelligence ML Service", version="2.0.0")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ---------------------------------------------------------------------------
# Prediction logging
# ---------------------------------------------------------------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [ml-service] %(levelname)s %(message)s",
)
logger = logging.getLogger("ml-service")

# Feature ranges seen during training, taken from the rep panels. Inputs outside
# these bounds land in the same terminal leaf of every tree, which makes the model
# return an identical value for every employee -- the signature of a caller sending
# the wrong unit (win rate as 0-100 instead of 0-1) or a partial period's totals.
TRAINING_RANGES: Dict[str, Dict[str, tuple]] = {
    "employee_yearly": {
        "revenue": (1.1e7, 2.9e7),
        "years_active": (0, 3),
        "deals_worked": (205, 374),
        "win_rate": (0.29, 0.46),
    },
    "employee_quarterly": {
        "deals_worked": (40, 110),
        "win_rate": (0.15, 0.55),
        "avg_deal_size": (2500, 5000),
        "avg_cycle_days": (60, 100),
        "revenue": (0, 8.0e6),
    },
}


def _log_prediction(kind: str, sales_agent: str, features: Dict[str, float]) -> None:
    """Record which employee was asked for and the exact vector built for them."""
    vector = ", ".join(f"{k}={v:.6g}" for k, v in features.items())
    logger.info("%s | sales_agent='%s' | features=[%s]", kind, sales_agent or "<missing>", vector)

    if not sales_agent:
        logger.warning(
            "%s called without a sales_agent. Without an employee identifier the "
            "caller cannot be building per-employee features.", kind,
        )

    out_of_range = [
        f"{name}={features[name]:.6g} (trained {lo:.6g}..{hi:.6g})"
        for name, (lo, hi) in TRAINING_RANGES.get(kind, {}).items()
        if name in features and not (lo <= features[name] <= hi)
    ]
    if out_of_range:
        logger.warning(
            "%s | sales_agent='%s' | features outside the training range: %s. "
            "Predictions here are unreliable and tend to be identical across employees.",
            kind, sales_agent or "<missing>", "; ".join(out_of_range),
        )


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
def _num(value: Optional[float], fallback: float) -> float:
    try:
        if value is None:
            return float(fallback)
        v = float(value)
        if math.isnan(v) or math.isinf(v):
            return float(fallback)
        return v
    except (TypeError, ValueError):
        return float(fallback)


def _win_rate_fraction(value: Optional[float], fallback: float) -> float:
    """
    Both employee models were trained on win rate as a fraction in [0,1].

    Callers historically sent a 0-100 percentage. Feeding 38.0 to a model that
    learned 0.38 pushes every rep far outside the training range, where the trees
    collapse onto one leaf and return the same number for everybody, so anything
    above 1 is treated as a percentage and scaled back down.
    """
    v = _num(value, fallback)
    if v > 1.0:
        logger.warning(
            "win_rate=%.6g looks like a 0-100 percentage; the models expect a 0-1 "
            "fraction. Converting to %.6g.", v, v / 100.0,
        )
        v = v / 100.0
    return max(0.0, min(1.0, v))


def _sample_std(values: List[float]) -> float:
    vals = [v for v in values if v is not None]
    if len(vals) < 2:
        return 0.0
    mean = sum(vals) / len(vals)
    var = sum((v - mean) ** 2 for v in vals) / (len(vals) - 1)  # ddof=1 (sample std)
    return math.sqrt(var)


# ---------------------------------------------------------------------------
# Request schemas (all accept the new true features AND the old fields for
# backward compatibility; missing values are derived with sensible defaults).
# ---------------------------------------------------------------------------
class CompanyRevenueRequest(BaseModel):
    # Old backward-compatible fields
    lag_1: float = 250000.0
    lag_2: float = 240000.0
    lag_3: float = 230000.0
    rolling_mean: Optional[float] = None
    # New lag-model fields (derived from the above when not supplied)
    lag_6: Optional[float] = None
    lag_12: Optional[float] = None
    rolling_mean_3: Optional[float] = None
    rolling_std_3: Optional[float] = None
    month: Optional[int] = None
    quarter: Optional[int] = None


class WinRateRequest(BaseModel):
    # New classifier inputs (numerical + categorical)
    deal_value_proposed: float = 3000.0
    employees: float = 3000.0
    sales_cycle_days: float = 60.0
    engage_month: Optional[int] = None
    engage_quarter: Optional[int] = None
    sector: Optional[str] = None
    product: Optional[str] = None
    regional_office: Optional[str] = None
    office_location: Optional[str] = None
    sales_agent: Optional[str] = None
    # Old backward-compatible fields (used only to derive month/quarter)
    month: Optional[int] = None
    quarter: Optional[int] = None
    is_quarter_end: Optional[int] = None


class EmployeePerformanceRequest(BaseModel):
    sales_agent: str = ""
    # New quarterly-model true features
    deals_worked: Optional[float] = None
    win_rate: Optional[float] = None
    avg_deal_size: Optional[float] = None
    avg_cycle_days: Optional[float] = None
    revenue: Optional[float] = None
    # Old backward-compatible fields (mapped to the true features when needed)
    total_deals_lag1: Optional[float] = None
    closed_deals_lag1: Optional[float] = None
    won_deals_lag1: Optional[float] = None
    total_revenue_lag1: Optional[float] = None
    average_deal_value_lag1: Optional[float] = None
    average_sales_cycle_lag1: Optional[float] = None
    win_rate_lag1: Optional[float] = None
    win_rate_rolling_3: Optional[float] = None
    revenue_rolling_3: Optional[float] = None
    deals_rolling_3: Optional[float] = None
    month_number: Optional[int] = None
    quarter: Optional[int] = None
    year: Optional[int] = None


class EmployeeRevenueRequest(BaseModel):
    sales_agent: str = ""
    # New yearly-model true features
    revenue: Optional[float] = None
    years_active: Optional[float] = None
    deals_worked: Optional[float] = None
    win_rate: Optional[float] = None
    # Old backward-compatible fields
    lag_1: Optional[float] = None
    lag_2: Optional[float] = None
    lag_3: Optional [float] = None
    lag_6: Optional[float] = None
    lag_12: Optional[float] = None
    rolling_mean_3: Optional[float] = None
    rolling_mean_6: Optional[float] = None
    rolling_mean_12: Optional[float] = None
    month_number: Optional[int] = None
    quarter: Optional[int] = None
    year: Optional[int] = None


class RevenueForecastRequest(BaseModel):
    periods: int = 3


# ---------------------------------------------------------------------------
# Health
# ---------------------------------------------------------------------------
@app.get("/health")
def health():
    return {
        "status": "healthy",
        "models_loaded": {
            "lag_revenue_model": lag_revenue_model is not None,
            "revenue_forecast_model": revenue_forecast_model is not None,
            "winrate_model": winrate_model is not None,
            "winrate_model_columns": bool(winrate_columns),
            "employee_quarterly_model": employee_quarterly_model is not None,
            "employee_yearly_model": employee_yearly_model is not None,
        },
        "forecast_fallback_csv": os.path.exists(_FORECAST_CSV),
    }


# ---------------------------------------------------------------------------
# Company revenue (lag revenue model — 9 features)
# ---------------------------------------------------------------------------
@app.post("/predict/company-revenue")
def predict_company_revenue(req: CompanyRevenueRequest):
    if lag_revenue_model is None:
        raise HTTPException(status_code=500, detail="Company revenue (lag) model not loaded")

    lag_1 = _num(req.lag_1, 250000.0)
    lag_2 = _num(req.lag_2, 240000.0)
    lag_3 = _num(req.lag_3, 230000.0)

    rolling_mean_3 = req.rolling_mean_3 if req.rolling_mean_3 is not None else req.rolling_mean
    rolling_mean_3 = _num(rolling_mean_3, (lag_1 + lag_2 + lag_3) / 3.0)
    rolling_std_3 = _num(req.rolling_std_3, _sample_std([lag_1, lag_2, lag_3]))
    lag_6 = _num(req.lag_6, lag_3)
    lag_12 = _num(req.lag_12, lag_6)
    month = int(_num(req.month, 1))
    quarter = int(_num(req.quarter, (month - 1) // 3 + 1))

    feature_values = {
        "lag_1": lag_1,
        "lag_2": lag_2,
        "lag_3": lag_3,
        "lag_6": lag_6,
        "lag_12": lag_12,
        "rolling_mean_3": rolling_mean_3,
        "rolling_std_3": rolling_std_3,
        "month": month,
        "quarter": quarter,
    }
    input_df = pd.DataFrame([[feature_values[f] for f in lag_revenue_features]], columns=list(lag_revenue_features))
    prediction = float(lag_revenue_model.predict(input_df)[0])

    return {
        "prediction_type": "company_revenue",
        "predicted_revenue": round(prediction, 2),
        "inputs": feature_values,
        "model_used": "lag_revenue_model.pkl",
    }


# ---------------------------------------------------------------------------
# Win rate / deal win probability (classifier + one-hot encoder metadata)
# ---------------------------------------------------------------------------
def _clamp_category(value: Optional[str], categories: List[str]) -> str:
    if value is not None:
        return value
    return categories[0] if len(categories) else ""


@app.post("/predict/win-rate")
def predict_win_rate(req: WinRateRequest):
    if winrate_model is None or not winrate_columns:
        raise HTTPException(status_code=500, detail="Win rate model or metadata not loaded")

    feature_cols_num: List[str] = winrate_columns.get("feature_cols_num", [])
    feature_cols_cat: List[str] = winrate_columns.get("feature_cols_cat", [])
    encoder = winrate_columns.get("encoder")
    engage_month = int(_num(req.engage_month if req.engage_month is not None else req.month, 1))
    engage_quarter = int(_num(
        req.engage_quarter if req.engage_quarter is not None else req.quarter,
        (engage_month - 1) // 3 + 1,
    ))

    num_lookup = {
        "deal_value_proposed": _num(req.deal_value_proposed, 3000.0),
        "employees": _num(req.employees, 3000.0),
        "sales_cycle_days": _num(req.sales_cycle_days, 60.0),
        "engage_month": engage_month,
        "engage_quarter": engage_quarter,
    }
    num_values = [num_lookup.get(c, 0.0) for c in feature_cols_num]

    cat_input = {
        "sector": req.sector,
        "product": req.product,
        "regional_office": req.regional_office,
        "office_location": req.office_location,
        "sales_agent": req.sales_agent,
    }
    categories_by_col = {}
    if encoder is not None and hasattr(encoder, "categories_"):
        for col, cats in zip(feature_cols_cat, encoder.categories_):
            categories_by_col[col] = list(cats)

    cat_row = {
        col: _clamp_category(cat_input.get(col), categories_by_col.get(col, []))
        for col in feature_cols_cat
    }

    try:
        cat_df = pd.DataFrame([cat_row])[feature_cols_cat]
        encoded = encoder.transform(cat_df)
        encoded_arr = encoded.toarray() if hasattr(encoded, "toarray") else np.asarray(encoded)
        row = np.concatenate([np.asarray(num_values, dtype=float), encoded_arr[0].astype(float)])
        # The classifier was fitted on a bare numpy array (no feature names), so
        # feed a 2D array in the exact `feature_names` order to avoid a mismatch warning.
        X = row.reshape(1, -1)
        classes = list(getattr(winrate_model, "classes_", [0.0, 1.0]))
        proba = winrate_model.predict_proba(X)[0]
        win_index = classes.index(1.0) if 1.0 in classes else (classes.index(1) if 1 in classes else len(classes) - 1)
        win_probability = float(proba[win_index]) * 100.0
    except Exception as exc:  # noqa: BLE001
        raise HTTPException(status_code=500, detail=f"Win rate prediction failed: {exc}")

    return {
        "prediction_type": "win_rate",
        "win_probability": round(win_probability, 2),
        # Backward-compatible field: same probability expressed as a percentage.
        "predicted_win_rate": round(win_probability, 2),
        "inputs": {**num_lookup, **cat_row},
        "model_used": "winrate_model.pkl",
    }


# ---------------------------------------------------------------------------
# Employee performance (quarterly model -> next-quarter revenue)
# ---------------------------------------------------------------------------
@app.post("/predict/employee-performance")
def predict_employee_performance(req: EmployeePerformanceRequest):
    if employee_quarterly_model is None:
        raise HTTPException(status_code=500, detail="Employee quarterly model not loaded")

    deals_worked = req.deals_worked
    if deals_worked is None:
        deals_worked = req.total_deals_lag1 if req.total_deals_lag1 is not None else req.closed_deals_lag1
    deals_worked = _num(deals_worked, 10.0)

    win_rate = req.win_rate if req.win_rate is not None else req.win_rate_lag1
    win_rate = _win_rate_fraction(win_rate, 0.38)

    avg_deal_size = req.avg_deal_size if req.avg_deal_size is not None else req.average_deal_value_lag1
    avg_deal_size = _num(avg_deal_size, 8000.0)

    avg_cycle_days = req.avg_cycle_days if req.avg_cycle_days is not None else req.average_sales_cycle_lag1
    avg_cycle_days = _num(avg_cycle_days, 30.0)

    revenue = req.revenue
    if revenue is None:
        revenue = req.total_revenue_lag1 if req.total_revenue_lag1 is not None else req.revenue_rolling_3
    revenue = _num(revenue, 75000.0)

    feature_values = {
        "deals_worked": deals_worked,
        "win_rate": win_rate,
        "avg_deal_size": avg_deal_size,
        "avg_cycle_days": avg_cycle_days,
        "revenue": revenue,
    }
    _log_prediction("employee_quarterly", req.sales_agent, feature_values)

    input_df = pd.DataFrame(
        [[feature_values[f] for f in employee_quarterly_features]],
        columns=list(employee_quarterly_features),
    )
    next_quarter_revenue = float(employee_quarterly_model.predict(input_df)[0])

    # Backward-compatible performance score: a genuine 0-100 percentage derived
    # from the rep's win rate (NOT the raw revenue). See MODEL_CONFIGURATION.md.
    # win_rate is a fraction internally, so scale it back up for display.
    performance_score = max(0.0, min(100.0, win_rate * 100.0))

    return {
        "prediction_type": "employee_performance",
        "sales_agent": req.sales_agent,
        "next_quarter_revenue": round(next_quarter_revenue, 2),
        "predicted_performance_score": round(performance_score, 1),
        "performance_score_basis": "win_rate_percentage",
        "inputs": feature_values,
        "model_used": "employee_quarterly_model.pkl",
    }


# ---------------------------------------------------------------------------
# Employee revenue (yearly model -> next-year revenue)
# ---------------------------------------------------------------------------
@app.post("/predict/employee-revenue")
def predict_employee_revenue(req: EmployeeRevenueRequest):
    if employee_yearly_model is None:
        raise HTTPException(status_code=500, detail="Employee yearly model not loaded")

    revenue = req.revenue
    if revenue is None:
        # Approximate annual revenue from monthly history when only old fields exist.
        if req.rolling_mean_12 is not None:
            revenue = _num(req.rolling_mean_12, 0.0) * 12.0
        elif req.lag_1 is not None:
            revenue = _num(req.lag_1, 0.0) * 12.0
    revenue = _num(revenue, 600000.0)

    years_active = _num(req.years_active, 2.0)
    deals_worked = _num(req.deals_worked, 40.0)
    win_rate = _win_rate_fraction(req.win_rate, 0.38)

    feature_values = {
        "revenue": revenue,
        "years_active": years_active,
        "deals_worked": deals_worked,
        "win_rate": win_rate,
    }
    _log_prediction("employee_yearly", req.sales_agent, feature_values)

    input_df = pd.DataFrame(
        [[feature_values[f] for f in employee_yearly_features]],
        columns=list(employee_yearly_features),
    )
    predicted_revenue = float(employee_yearly_model.predict(input_df)[0])

    return {
        "prediction_type": "employee_revenue",
        "sales_agent": req.sales_agent,
        "predicted_revenue": round(predicted_revenue, 2),
        "prediction_horizon": "next_year",
        "inputs": feature_values,
        "model_used": "employee_yearly_model.pkl",
    }


# ---------------------------------------------------------------------------
# Revenue forecast (Holt-Winters, 3-month horizon) — GET and POST
# ---------------------------------------------------------------------------
def _forecast_from_csv(periods: int) -> Optional[List[Dict[str, Any]]]:
    if not os.path.exists(_FORECAST_CSV):
        return None
    try:
        df = pd.read_csv(_FORECAST_CSV)
        items = []
        for _, r in df.head(periods).iterrows():
            items.append({
                "month": str(r.get("month", "")),
                "forecast_revenue": round(float(r.get("forecast_revenue", 0.0)), 2),
            })
        return items
    except Exception as exc:  # noqa: BLE001
        print(f"[ml-service] forecast CSV read failed: {exc}")
        return None


def _build_forecast(periods: int) -> Dict[str, Any]:
    periods = max(1, min(12, int(periods)))

    # Primary: live Holt-Winters model if it loaded cleanly.
    if revenue_forecast_model is not None and hasattr(revenue_forecast_model, "forecast"):
        try:
            values = list(revenue_forecast_model.forecast(periods))
            items = [{"forecast_revenue": round(float(v), 2)} for v in values]
            # Attach month labels from the precomputed CSV when available.
            csv_items = _forecast_from_csv(periods) or []
            for i, item in enumerate(items):
                item["month"] = csv_items[i]["month"] if i < len(csv_items) else f"M+{i + 1}"
            return {
                "prediction_type": "revenue_forecast",
                "horizon_months": periods,
                "forecast": items,
                "total": round(sum(v["forecast_revenue"] for v in items), 2),
                "model_used": "revenue_forecast_model.pkl",
                "source": "holt_winters_model",
            }
        except Exception as exc:  # noqa: BLE001
            print(f"[ml-service] Holt-Winters forecast failed, using CSV fallback: {exc}")

    # Fallback: precomputed forecast CSV produced by the training notebook.
    csv_items = _forecast_from_csv(periods)
    if csv_items:
        return {
            "prediction_type": "revenue_forecast",
            "horizon_months": len(csv_items),
            "forecast": csv_items,
            "total": round(sum(v["forecast_revenue"] for v in csv_items), 2),
            "model_used": "revenue_forecast_next_3_months.csv",
            "source": "precomputed_csv",
        }

    raise HTTPException(status_code=500, detail="Revenue forecast model and fallback CSV are both unavailable")


@app.get("/forecast/revenue")
def forecast_revenue_get(periods: int = 3):
    return _build_forecast(periods)


@app.post("/forecast/revenue")
def forecast_revenue_post(req: RevenueForecastRequest):
    return _build_forecast(req.periods)


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5001)
