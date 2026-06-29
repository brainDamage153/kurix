import { Link } from "react-router-dom";
import { api } from "../api.js";
import { useAsync, Loading, ErrorBox } from "../components/Async.jsx";

const statusLabel = {
  Active: "Activa",
  Escalated: "Escalada",
  Closed: "Cerrada"
};

function StatusBadge({ status }) {
  return <span className={"badge badge-" + status.toLowerCase()}>{statusLabel[status] || status}</span>;
}

function formatDate(value) {
  if (!value) return "—";
  return new Date(value).toLocaleString("es");
}

export default function Conversations() {
  const { data, error, loading } = useAsync(() => api.conversations());

  if (loading) return <Loading />;
  if (error) return <ErrorBox message={error} />;

  return (
    <div>
      <h1>Conversaciones</h1>
      {data.length === 0 ? (
        <p className="muted">Todavía no hay conversaciones.</p>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Sesión</th>
              <th>Estado</th>
              <th>Inicio</th>
              <th>Último mensaje</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data.map((c) => (
              <tr key={c.id}>
                <td className="mono">{c.sessionId.slice(0, 12)}…</td>
                <td><StatusBadge status={c.status} /></td>
                <td>{formatDate(c.startedAt)}</td>
                <td>{formatDate(c.lastMessageAt)}</td>
                <td><Link className="link" to={`/conversations/${c.id}`}>Ver</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
