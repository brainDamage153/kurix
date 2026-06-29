import { Link, useParams } from "react-router-dom";
import { api } from "../api.js";
import { useAsync, Loading, ErrorBox } from "../components/Async.jsx";

const roleLabel = {
  User: "Usuario",
  Assistant: "Asistente",
  Tool: "Herramienta",
  System: "Sistema"
};

export default function ConversationDetail() {
  const { id } = useParams();
  const { data, error, loading } = useAsync(() => api.conversation(id), [id]);

  if (loading) return <Loading />;
  if (error) return <ErrorBox message={error} />;

  return (
    <div>
      <Link className="link" to="/conversations">&larr; Conversaciones</Link>
      <h1>Conversación</h1>
      <p className="muted mono">Sesión: {data.sessionId}</p>

      <div className="thread">
        {data.messages.map((m, i) => (
          <div key={i} className={"turn turn-" + m.role.toLowerCase()}>
            <div className="turn-meta">
              {roleLabel[m.role] || m.role}
              {m.toolName ? ` · ${m.toolName}` : ""}
              <span className="muted"> · {new Date(m.createdAt).toLocaleString("es")}</span>
            </div>
            <div className="turn-body">{m.content}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
