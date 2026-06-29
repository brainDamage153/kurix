import { Navigate, Route, Routes } from "react-router-dom";
import { getToken } from "./api.js";
import Layout from "./components/Layout.jsx";
import Login from "./pages/Login.jsx";
import Metrics from "./pages/Metrics.jsx";
import Conversations from "./pages/Conversations.jsx";
import ConversationDetail from "./pages/ConversationDetail.jsx";
import Knowledge from "./pages/Knowledge.jsx";
import Settings from "./pages/Settings.jsx";

function RequireAuth({ children }) {
  return getToken() ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/metrics" replace />} />
        <Route path="metrics" element={<Metrics />} />
        <Route path="conversations" element={<Conversations />} />
        <Route path="conversations/:id" element={<ConversationDetail />} />
        <Route path="knowledge" element={<Knowledge />} />
        <Route path="settings" element={<Settings />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
