import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";

import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import EmployeePerformance from "./pages/EmployeePerformance";
import AdminLayout from "./layouts/AdminLayout";
import RevenueForecast from "./pages/RevenueForecast";
import EmployeeRevenue from "./pages/EmployeeRevenue";
import Employees from "./pages/Employees";
import Deals from "./pages/Deals";
import Users from "./pages/Users";
import { AuthProvider, useAuth } from "./context/AuthContext";
import { DatasetProvider } from "./context/DatasetContext";

// Protected Route Component
const ProtectedRoute = ({ children, requireSuperadmin = false }) => {
  const { isAuthenticated, isSuperadmin, loading } = useAuth();

  if (loading) {
    return <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', background: '#0f172a', color: '#fff' }}>Loading Sales Intelligence...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requireSuperadmin && !isSuperadmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
};

export default function App() {
  return (
    <AuthProvider>
      <DatasetProvider>
        <BrowserRouter>
          <Routes>

            {/* PUBLIC LOGIN */}
            <Route path="/login" element={<Login />} />

            {/* PROTECTED APP LAYOUT */}
            <Route
              element={
                <ProtectedRoute>
                  <AdminLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/revenue-forecast" element={<RevenueForecast />} />
              <Route path="/employee-revenue" element={<EmployeeRevenue />} />
              <Route path="/employee-performance" element={<EmployeePerformance />} />
              <Route path="/deals" element={<Deals />} />
              <Route path="/employees" element={<Employees />} />

              {/* SUPERADMIN ONLY */}
              <Route
                path="/users"
                element={
                  <ProtectedRoute requireSuperadmin={true}>
                    <Users />
                  </ProtectedRoute>
                }
              />
            </Route>

            {/* DEFAULT REDIRECT */}
            <Route path="/" element={<ProtectedRoute><Navigate to="/dashboard" replace /></ProtectedRoute>} />
            {/* UNKNOWN ROUTE */}
            <Route path="*" element={<Navigate to="/login" replace />} />
          </Routes>
        </BrowserRouter>
      </DatasetProvider>
    </AuthProvider>
  );
}