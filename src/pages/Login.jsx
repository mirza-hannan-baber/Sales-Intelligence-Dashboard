import { useState } from "react";
import { Eye, EyeOff, LockKeyhole, Mail } from "lucide-react";

export default function Login() {
  const [showPassword, setShowPassword] = useState(false);

  const handleSubmit = (e) => {
    e.preventDefault();

    // Backend baad mein connect hoga
    console.log("Login submitted");
  };

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

          <span className="eyebrow">
            SMARTER SALES DECISIONS
          </span>

          <h1>
            Turn your sales data
            <br />
            into <span>better decisions.</span>
          </h1>

          <p>
            Monitor revenue, analyze employee performance,
            forecast future sales and make smarter decisions
            with AI-powered analytics.
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

        <div className="login-footer">
          © 2026 Sales Intelligence
        </div>

      </div>


      {/* RIGHT SIDE */}
      <div className="login-right">

        <div className="login-card">

          <div className="mobile-logo">
            SI
          </div>

          <div className="login-heading">
            <h1>Welcome back</h1>

            <p>
              Sign in to your admin account
            </p>
          </div>

          <form onSubmit={handleSubmit}>

            {/* EMAIL */}
            <div className="form-group">

              <label htmlFor="email">
                Email address
              </label>

              <div className="input-wrapper">

                <Mail size={18} />

                <input
                  id="email"
                  type="email"
                  placeholder="admin@company.com"
                  required
                />

              </div>

            </div>


            {/* PASSWORD */}
            <div className="form-group">

              <div className="label-row">

                <label htmlFor="password">
                  Password
                </label>

                <button
                  type="button"
                  className="forgot-btn"
                >
                  Forgot password?
                </button>

              </div>

              <div className="input-wrapper">

                <LockKeyhole size={18} />

                <input
                  id="password"
                  type={
                    showPassword
                      ? "text"
                      : "password"
                  }
                  placeholder="Enter your password"
                  required
                />

                <button
                  type="button"
                  className="password-toggle"
                  onClick={() =>
                    setShowPassword(!showPassword)
                  }
                >
                  {showPassword ? (
                    <EyeOff size={18} />
                  ) : (
                    <Eye size={18} />
                  )}
                </button>

              </div>

            </div>


            {/* REMEMBER */}
            <div className="remember-row">

              <label className="remember-label">

                <input type="checkbox" />

                <span>
                  Remember me
                </span>

              </label>

            </div>


            {/* LOGIN */}
            <button
              type="submit"
              className="login-button"
            >
              Sign in
            </button>

          </form>


          <div className="login-note">

            <LockKeyhole size={15} />

            <span>
              Your account is managed by your
              organization administrator.
            </span>

          </div>

        </div>

      </div>

    </div>
  );
}