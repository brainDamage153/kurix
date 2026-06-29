import { useEffect, useState, useCallback } from "react";

/**
 * Minimal data-loading helper: runs an async loader and exposes
 * { data, error, loading, reload }.
 */
export function useAsync(loader, deps = []) {
  const [state, setState] = useState({ data: null, error: null, loading: true });

  const run = useCallback(() => {
    let active = true;
    setState((s) => ({ ...s, loading: true, error: null }));
    loader()
      .then((data) => active && setState({ data, error: null, loading: false }))
      .catch((err) => active && setState({ data: null, error: err.message, loading: false }));
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(run, [run]);

  const reload = useCallback(() => run(), [run]);
  return { ...state, reload };
}

export function Loading() {
  return <div className="muted pad">Cargando…</div>;
}

export function ErrorBox({ message }) {
  return <div className="alert error">{message}</div>;
}
