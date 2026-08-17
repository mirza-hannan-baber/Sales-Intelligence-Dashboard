import { Outlet } from "react-router-dom";
import Sidebar from "../components/Sidebar";
import Header from "../components/Header";
import Chatbot from "../components/Chatbot/Chatbot";
import ErrorBoundary from "../components/ErrorBoundary";

export default function AdminLayout() {
  return (
    <div className="admin-layout">
      <Sidebar />

      <div className="admin-main">
        <Header />

        <main className="admin-content">
          <ErrorBoundary>
            <Outlet />
          </ErrorBoundary>
        </main>
      </div>

      <Chatbot />
    </div>
  );
}