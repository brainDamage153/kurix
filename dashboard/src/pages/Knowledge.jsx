import { useState } from "react";
import { api } from "../api.js";
import { useAsync, Loading, ErrorBox } from "../components/Async.jsx";

function formatDate(value) {
  return value ? new Date(value).toLocaleString("es") : "—";
}

export default function Knowledge() {
  const { data, error, loading, reload } = useAsync(() => api.documents());
  const [fileName, setFileName] = useState("");
  const [content, setContent] = useState("");
  const [busy, setBusy] = useState(false);
  const [formError, setFormError] = useState(null);

  function onFile(e) {
    const file = e.target.files && e.target.files[0];
    if (!file) return;
    setFileName(file.name);
    const reader = new FileReader();
    reader.onload = () => setContent(String(reader.result || ""));
    reader.readAsText(file);
  }

  async function upload(e) {
    e.preventDefault();
    setFormError(null);
    if (!fileName.trim() || !content.trim()) {
      setFormError("Ingresá un nombre y contenido.");
      return;
    }
    setBusy(true);
    try {
      await api.ingest(fileName.trim(), content);
      setFileName("");
      setContent("");
      reload();
    } catch (err) {
      setFormError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function remove(id) {
    if (!window.confirm("¿Eliminar este documento y sus fragmentos?")) return;
    await api.deleteDocument(id);
    reload();
  }

  return (
    <div>
      <h1>Base de conocimiento</h1>

      <form className="card form" onSubmit={upload}>
        <h2>Subir documento</h2>
        {formError && <div className="alert error">{formError}</div>}
        <label>Archivo (.txt, .md) o pegá el texto abajo</label>
        <input type="file" accept=".txt,.md,text/plain,text/markdown" onChange={onFile} />

        <label>Nombre</label>
        <input value={fileName} onChange={(e) => setFileName(e.target.value)} placeholder="faq.txt" />

        <label>Contenido</label>
        <textarea
          rows={6}
          value={content}
          onChange={(e) => setContent(e.target.value)}
          placeholder="Pegá aquí el contenido del documento…"
        />

        <button type="submit" disabled={busy}>
          {busy ? "Subiendo…" : "Subir e indexar"}
        </button>
      </form>

      <h2>Documentos</h2>
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorBox message={error} />
      ) : data.length === 0 ? (
        <p className="muted">No hay documentos cargados.</p>
      ) : (
        <table className="table">
          <thead>
            <tr>
              <th>Archivo</th>
              <th>Estado</th>
              <th className="num">Fragmentos</th>
              <th>Ingestado</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data.map((d) => (
              <tr key={d.id}>
                <td>{d.fileName}</td>
                <td>{d.status}{d.errorMessage ? ` (${d.errorMessage})` : ""}</td>
                <td className="num">{d.chunkCount}</td>
                <td>{formatDate(d.ingestedAt)}</td>
                <td>
                  <button className="link danger" onClick={() => remove(d.id)}>Eliminar</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
