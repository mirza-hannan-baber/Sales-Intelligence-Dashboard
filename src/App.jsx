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


export default function App() {
  return (
    <BrowserRouter>

      <Routes>

        {/* LOGIN */}
        <Route
          path="/login"
          element={<Login />}
        />


        {/* ADMIN AREA */}
        <Route
          element={<AdminLayout />}
        >

          <Route
            path="/dashboard"
            element={<Dashboard />}
          />
          <Route
            path="/employee-performance"
            element={<EmployeePerformance />}
          />
        </Route>


        {/* DEFAULT */}
        <Route
          path="/"
          element={
            <Navigate
              to="/login"
              replace
            />
          }
        />


        {/* UNKNOWN ROUTE */}
        <Route
          path="*"
          element={
            <Navigate
              to="/login"
              replace
            />
          }
        />

      </Routes>

    </BrowserRouter>
  );
}