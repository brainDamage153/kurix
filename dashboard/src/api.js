// Thin fetch wrapper. The dashboard talks to the Kurix API with the dashboard
// JWT (stored in localStorage). In dev, /api is proxied by Vite.

const BASE = import.meta.env.VITE_API_URL || "";
const TOKEN_KEY = "kurix_token";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(token) {
  localStorage.setItem(TOKEN_KEY, token);
}
export function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}

async function request(path, options = {}) {
  const headers = { "Content-Type": "application/json", ...(options.headers || {}) };
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(BASE + path, { ...options, headers });

  if (res.status === 401) {
    clearToken();
    if (!window.location.pathname.endsWith("/login")) {
      window.location.assign("/login");
    }
    throw new Error("No autorizado");
  }
  if (!res.ok) {
    let message = `Error ${res.status}`;
    try {
      const body = await res.json();
      if (body && body.error) message = body.error;
    } catch (_) {}
    throw new Error(message);
  }
  if (res.status === 204) return null;
  return res.json();
}

export const api = {
  login: (email, password) =>
    request("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),
  metrics: () => request("/api/metrics"),
  conversations: () => request("/api/conversations"),
  conversation: (id) => request(`/api/conversations/${id}`),
  documents: () => request("/api/knowledge/documents"),
  ingest: (fileName, content, metadata) =>
    request("/api/knowledge/documents", {
      method: "POST",
      body: JSON.stringify({ fileName, content, metadata })
    }),
  deleteDocument: (id) => request(`/api/knowledge/documents/${id}`, { method: "DELETE" }),
  getSettings: () => request("/api/tenant/settings"),
  updateSettings: (settings) =>
    request("/api/tenant/settings", { method: "PUT", body: JSON.stringify(settings) })
};
