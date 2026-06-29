import { useEffect, useState } from "react";
import { api } from "../api.js";
import { Loading, ErrorBox } from "../components/Async.jsx";

// Tools shipped by Module 1. The dashboard lets the tenant enable/disable each.
const KNOWN_TOOLS = [
  { name: "check_availability", label: "Consultar disponibilidad" },
  { name: "create_booking", label: "Crear reserva" },
  { name: "search_inventory", label: "Buscar inventario" },
  { name: "escalate_to_human", label: "Escalar a humano" }
];

export default function Settings() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [saved, setSaved] = useState(false);
  const [persona, setPersona] = useState("");
  const [fallback, setFallback] = useState("");
  const [webhook, setWebhook] = useState("");
  const [enabled, setEnabled] = useState({});

  useEffect(() => {
    let active = true;
    api
      .getSettings()
      .then((s) => {
        if (!active) return;
        setPersona(s.persona || "");
        setFallback(s.fallbackMessage || "");
        setWebhook(s.escalationWebhookUrl || "");
        // null => all tools enabled.
        const list = s.enabledTools;
        const map = {};
        KNOWN_TOOLS.forEach((t) => {
          map[t.name] = list === null || list === undefined ? true : list.includes(t.name);
        });
        setEnabled(map);
        setLoading(false);
      })
      .catch((err) => active && (setError(err.message), setLoading(false)));
    return () => {
      active = false;
    };
  }, []);

  function toggle(name) {
    setEnabled((e) => ({ ...e, [name]: !e[name] }));
    setSaved(false);
  }

  async function save(e) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    const enabledTools = KNOWN_TOOLS.filter((t) => enabled[t.name]).map((t) => t.name);
    try {
      await api.updateSettings({
        persona,
        fallbackMessage: fallback,
        escalationWebhookUrl: webhook || null,
        enabledTools
      });
      setSaved(true);
    } catch (err) {
      setError(err.message);
    }
  }

  if (loading) return <Loading />;

  return (
    <div>
      <h1>Configuración</h1>
      {error && <ErrorBox message={error} />}
      {saved && <div className="alert success">Cambios guardados.</div>}

      <form className="card form" onSubmit={save}>
        <label>Persona del bot</label>
        <textarea rows={4} value={persona} onChange={(e) => setPersona(e.target.value)} />

        <label>Mensaje de respaldo (fallback)</label>
        <textarea rows={2} value={fallback} onChange={(e) => setFallback(e.target.value)} />

        <label>Webhook de escalamiento (opcional)</label>
        <input
          type="url"
          value={webhook}
          onChange={(e) => setWebhook(e.target.value)}
          placeholder="https://…"
        />

        <label>Herramientas habilitadas</label>
        <div className="tools">
          {KNOWN_TOOLS.map((t) => (
            <label key={t.name} className="checkbox">
              <input type="checkbox" checked={!!enabled[t.name]} onChange={() => toggle(t.name)} />
              {t.label} <span className="muted mono">({t.name})</span>
            </label>
          ))}
        </div>

        <button type="submit">Guardar cambios</button>
      </form>
    </div>
  );
}
