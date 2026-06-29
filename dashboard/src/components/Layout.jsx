import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { clearToken } from "../api.js";

const links = [
  { to: "/metrics", label: "Métricas" },
  { to: "/conversations", label: "Conversaciones" },
  { to: "/knowledge", label: "Base de conocimiento" },
  { to: "/settings", label: "Configuración" }
];

export default function Layout() {
  const navigate = useNavigate();

  function logout() {
    clearToken();
    navigate("/login", { replace: true });
  }

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="brand">Kurix</div>
        <nav>
          {links.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              className={({ isActive }) => "nav-link" + (isActive ? " active" : "")}
            >
              {l.label}
            </NavLink>
          ))}
        </nav>
        <button className="logout" onClick={logout}>
          Cerrar sesión
        </button>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
