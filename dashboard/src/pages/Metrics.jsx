import { api } from "../api.js";
import { useAsync, Loading, ErrorBox } from "../components/Async.jsx";

function Stat({ label, value, sub }) {
  return (
    <div className="card stat">
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
      {sub && <div className="stat-sub muted">{sub}</div>}
    </div>
  );
}

export default function Metrics() {
  const { data, error, loading } = useAsync(() => api.metrics());

  if (loading) return <Loading />;
  if (error) return <ErrorBox message={error} />;

  const m = data;
  const escalationPct = (m.escalationRate * 100).toFixed(1) + "%";

  return (
    <div>
      <h1>Métricas</h1>

      <div className="grid">
        <Stat label="Conversaciones" value={m.totalConversations} />
        <Stat
          label="Tasa de escalamiento"
          value={escalationPct}
          sub={`${m.escalatedConversations} escaladas`}
        />
        <Stat
          label="Tokens"
          value={(m.totalTokensIn + m.totalTokensOut).toLocaleString("es")}
          sub={`${m.totalTokensIn.toLocaleString("es")} in · ${m.totalTokensOut.toLocaleString("es")} out`}
        />
        <Stat label="Costo estimado" value={`US$ ${m.estimatedCostUsd.toFixed(4)}`} />
      </div>

      <h2>Preguntas frecuentes</h2>
      {m.frequentQuestions.length === 0 ? (
        <p className="muted">Todavía no hay preguntas registradas.</p>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Pregunta</th>
              <th className="num">Veces</th>
            </tr>
          </thead>
          <tbody>
            {m.frequentQuestions.map((q, i) => (
              <tr key={i}>
                <td>{q.question}</td>
                <td className="num">{q.count}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
