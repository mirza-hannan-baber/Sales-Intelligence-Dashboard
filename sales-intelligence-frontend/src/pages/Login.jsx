import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Eye, EyeOff, LockKeyhole, Mail, AlertCircle } from "lucide-react";
import { useAuth } from "../context/AuthContext";

export default function Login() {
  // const [email, setEmail] = useState("admin@company.com");
  // const [password, setPassword] = useState("Admin123!");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      await login(email, password);
      navigate("/dashboard");
    } catch (err) {
      setError(
        err.response?.data?.message ||
          "Invalid email or password. Please check your credentials."
      );
    } finally {
      setLoading(false);
    }
  };

  // const handleQuickLogin = (role) => {
  //   if (role === "admin") {
  //     setEmail("admin@company.com");
  //     setPassword("Admin123!");
  //   } else {
  //     setEmail("user@company.com");
  //     setPassword("User123!");
  //   }
  // };

  return (
    <div className="login-page">

      {/* LEFT SIDE */}
      <div className="login-left">
        <div className="brand">
          <div className="brand-logo">SI</div>
          <div>
            <h2>Sales Intelligence</h2>
            <p>AI-Powered Sales Analytics</p>
          </div>
        </div>

        <div className="login-left-content">
          <span className="eyebrow">SMARTER SALES DECISIONS</span>
          <h1>
            Turn your sales data
            <br />
            into <span>better decisions.</span>
          </h1>
          <p>
            Monitor revenue, analyze employee performance, forecast future sales
            and make smarter decisions with AI-powered analytics.
          </p>

          <div className="feature-list">
            <div className="feature-item">
              <div className="feature-icon">↗</div>
              <div>
                <strong>Revenue Forecasting</strong>
                <span>Predict future sales revenue</span>
              </div>
            </div>

            <div className="feature-item">
              <div className="feature-icon">◎</div>
              <div>
                <strong>Employee Analytics</strong>
                <span>Track employee performance</span>
              </div>
            </div>

            <div className="feature-item">
              <div className="feature-icon">✓</div>
              <div>
                <strong>Deal Intelligence</strong>
                <span>Predict individual deal outcomes</span>
              </div>
            </div>
          </div>
        </div>

        <div className="login-footer">© 2026 Sales Intelligence</div>
      </div>

      {/* RIGHT SIDE */}
      <div className="login-right">
        <div className="login-card">
          <div className="mobile-logo">SI</div>

          <div className="login-heading">
            <h1>Welcome back</h1>
            <p>Sign in to access your dashboard</p>
          </div>

          {error && (
            <div
              style={{
                display: "flex",
                alignItems: "center",
                gap: "8px",
                padding: "10px 14px",
                background: "#fef2f2",
                border: "1px solid #fca5a5",
                borderRadius: "8px",
                color: "#991b1b",
                marginBottom: "16px",
                fontSize: "0.875rem",
              }}
            >
              <AlertCircle size={18} />
              <span>{error}</span>
            </div>
          )}

          {/* Quick Preset Buttons */}
          {/* <div style={{ display: "flex", gap: "8px", marginBottom: "16px" }}>
            <button
              type="button"
              onClick={() => handleQuickLogin("admin")}
              style={{
                flex: 1,
                padding: "6px 10px",
                fontSize: "0.8rem",
                background: "#e0e7ff",
                color: "#3730a3",
                border: "1px solid #c7d2fe",
                borderRadius: "6px",
                cursor: "pointer",
              }}
            >
              Demo Superadmin
            </button>
            <button
              type="button"
              onClick={() => handleQuickLogin("user")}
              style={{
                flex: 1,
                padding: "6px 10px",
                fontSize: "0.8rem",
                background: "#d1fae5",
                color: "#065f46",
                border: "1px solid #a7f3d0",
                borderRadius: "6px",
                cursor: "pointer",
              }}
            >
              Demo Standard User
            </button>
          </div> */}

          <form onSubmit={handleSubmit}>
            {/* EMAIL */}
            <div className="form-group">
              <label htmlFor="email">Email address</label>
              <div className="input-wrapper">
                <Mail size={18} />
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Enter your email address "
                  required
                />
              </div>
            </div>

            {/* PASSWORD */}
            <div className="form-group">
              <div className="label-row">
                <label htmlFor="password">Password</label>
              </div>

              <div className="input-wrapper">
                <LockKeyhole size={18} />
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Enter your password"
                  required
                />
                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </div>

            {/* LOGIN */}
            <button
              type="submit"
              className="login-button"
              disabled={loading}
              style={{ opacity: loading ? 0.7 : 1 }}
            >
              {loading ? "Signing in..." : "Sign in"}
            </button>
          </form>

          
        </div>
      </div>
    </div>
  );
}