/*!
 * Kurix Chat Widget — vanilla JS, sin dependencias.
 *
 * Uso (2 líneas):
 *   <script src="https://cdn.kurix.io/widget.js" data-tenant="API_KEY"></script>
 *
 * Atributos del <script>:
 *   data-tenant   (requerido)  API key del tenant.
 *   data-api-url  (opcional)   Base URL de la API. Por defecto: origen del script.
 *   data-title    (opcional)   Título del header. Por defecto "Asistente".
 *   data-color    (opcional)   Color de acento. Por defecto "#4f46e5".
 *   data-greeting (opcional)   Mensaje de bienvenida.
 */
(function () {
  "use strict";

  var script = document.currentScript;
  if (!script) {
    // Fallback: último <script> con data-tenant.
    var scripts = document.querySelectorAll("script[data-tenant]");
    script = scripts[scripts.length - 1];
  }
  if (!script) return;

  var apiKey = script.getAttribute("data-tenant");
  if (!apiKey) {
    console.error("[Kurix] Falta el atributo data-tenant (API key).");
    return;
  }

  function attr(name, fallback) {
    var v = script.getAttribute(name);
    return v === null || v === "" ? fallback : v;
  }

  var apiUrl = attr("data-api-url", null);
  if (!apiUrl) {
    try {
      apiUrl = new URL(script.src).origin;
    } catch (e) {
      apiUrl = "";
    }
  }
  apiUrl = apiUrl.replace(/\/+$/, "");

  var config = {
    apiKey: apiKey,
    chatUrl: apiUrl + "/api/chat",
    title: attr("data-title", "Asistente"),
    color: attr("data-color", "#4f46e5"),
    greeting: attr("data-greeting", "¡Hola! ¿En qué puedo ayudarte?")
  };

  var STORAGE_KEY = "kurix_session_" + apiKey.slice(-8);

  // ---- State ----
  var state = {
    open: false,
    sending: false,
    escalated: false,
    sessionId: safeGet(STORAGE_KEY)
  };

  function safeGet(k) {
    try { return window.localStorage.getItem(k); } catch (e) { return null; }
  }
  function safeSet(k, v) {
    try { window.localStorage.setItem(k, v); } catch (e) {}
  }

  // ---- Styles ----
  var css =
    ".kurix-root{position:fixed;bottom:20px;right:20px;z-index:2147483000;" +
    "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif}" +
    ".kurix-bubble{width:60px;height:60px;border-radius:50%;border:none;cursor:pointer;" +
    "box-shadow:0 4px 14px rgba(0,0,0,.25);display:flex;align-items:center;justify-content:center;" +
    "color:#fff;transition:transform .15s ease}" +
    ".kurix-bubble:hover{transform:scale(1.06)}" +
    ".kurix-bubble svg{width:28px;height:28px}" +
    ".kurix-panel{position:absolute;bottom:76px;right:0;width:360px;max-width:calc(100vw - 40px);" +
    "height:520px;max-height:calc(100vh - 120px);background:#fff;border-radius:14px;overflow:hidden;" +
    "box-shadow:0 12px 40px rgba(0,0,0,.22);display:none;flex-direction:column}" +
    ".kurix-panel.kurix-show{display:flex}" +
    ".kurix-header{padding:16px;color:#fff;font-weight:600;font-size:15px;display:flex;" +
    "align-items:center;justify-content:space-between}" +
    ".kurix-close{background:none;border:none;color:#fff;cursor:pointer;font-size:20px;line-height:1;opacity:.85}" +
    ".kurix-close:hover{opacity:1}" +
    ".kurix-banner{display:none;padding:8px 16px;background:#fff7ed;color:#9a3412;font-size:12.5px;" +
    "border-bottom:1px solid #fed7aa}" +
    ".kurix-banner.kurix-show{display:block}" +
    ".kurix-msgs{flex:1;overflow-y:auto;padding:16px;background:#f8fafc;display:flex;flex-direction:column;gap:10px}" +
    ".kurix-msg{max-width:80%;padding:10px 13px;border-radius:14px;font-size:14px;line-height:1.4;white-space:pre-wrap;word-wrap:break-word}" +
    ".kurix-user{align-self:flex-end;color:#fff;border-bottom-right-radius:4px}" +
    ".kurix-bot{align-self:flex-start;background:#fff;color:#1f2937;border:1px solid #e5e7eb;border-bottom-left-radius:4px}" +
    ".kurix-typing{align-self:flex-start;color:#6b7280;font-size:13px;font-style:italic}" +
    ".kurix-input{display:flex;border-top:1px solid #e5e7eb;padding:10px;gap:8px;background:#fff}" +
    ".kurix-input textarea{flex:1;resize:none;border:1px solid #d1d5db;border-radius:10px;padding:9px 11px;" +
    "font-size:14px;font-family:inherit;outline:none;max-height:90px}" +
    ".kurix-input textarea:focus{border-color:var(--kurix-accent)}" +
    ".kurix-send{border:none;border-radius:10px;color:#fff;cursor:pointer;padding:0 14px;font-size:14px;font-weight:600}" +
    ".kurix-send:disabled{opacity:.5;cursor:default}" +
    ".kurix-footer{text-align:center;font-size:10.5px;color:#9ca3af;padding:6px}" +
    "@media (max-width:480px){.kurix-panel{width:calc(100vw - 24px);height:calc(100vh - 100px)}" +
    ".kurix-root{bottom:12px;right:12px}}";

  // ---- DOM ----
  var root, panel, msgs, banner, textarea, sendBtn, bubble;

  function el(tag, cls, html) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (html != null) e.innerHTML = html;
    return e;
  }

  function build() {
    var style = el("style");
    style.textContent = css;
    document.head.appendChild(style);

    root = el("div", "kurix-root");
    root.style.setProperty("--kurix-accent", config.color);

    // Panel
    panel = el("div", "kurix-panel");

    var header = el("div", "kurix-header");
    header.style.background = config.color;
    header.appendChild(el("span", null, escapeHtml(config.title)));
    var close = el("button", "kurix-close", "&times;");
    close.setAttribute("aria-label", "Cerrar");
    close.onclick = toggle;
    header.appendChild(close);
    panel.appendChild(header);

    banner = el("div", "kurix-banner",
      "Esta conversación fue derivada a un agente humano. Te responderán a la brevedad.");
    panel.appendChild(banner);

    msgs = el("div", "kurix-msgs");
    msgs.setAttribute("role", "log");
    msgs.setAttribute("aria-live", "polite");
    panel.appendChild(msgs);

    var inputRow = el("div", "kurix-input");
    textarea = el("textarea");
    textarea.rows = 1;
    textarea.placeholder = "Escribí tu mensaje…";
    textarea.setAttribute("aria-label", "Mensaje");
    textarea.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        send();
      }
    });
    sendBtn = el("button", "kurix-send", "Enviar");
    sendBtn.style.background = config.color;
    sendBtn.onclick = send;
    inputRow.appendChild(textarea);
    inputRow.appendChild(sendBtn);
    panel.appendChild(inputRow);

    panel.appendChild(el("div", "kurix-footer", "Powered by Kurix"));
    root.appendChild(panel);

    // Bubble
    bubble = el("button", "kurix-bubble");
    bubble.style.background = config.color;
    bubble.setAttribute("aria-label", "Abrir chat");
    bubble.innerHTML =
      '<svg viewBox="0 0 24 24" fill="currentColor"><path d="M12 3C6.5 3 2 6.8 2 11.5c0 2.1.9 4 2.4 5.4-.1 1.2-.6 2.6-1.4 3.6 1.6-.2 3.2-.8 4.4-1.7 1.4.6 2.9.9 4.6.9 5.5 0 10-3.8 10-8.6S17.5 3 12 3z"/></svg>';
    bubble.onclick = toggle;
    root.appendChild(bubble);

    document.body.appendChild(root);

    if (config.greeting) {
      addMessage("bot", config.greeting);
    }
  }

  // ---- UI helpers ----
  function toggle() {
    state.open = !state.open;
    panel.classList.toggle("kurix-show", state.open);
    bubble.setAttribute("aria-label", state.open ? "Cerrar chat" : "Abrir chat");
    if (state.open) setTimeout(function () { textarea.focus(); }, 50);
  }

  function addMessage(kind, text) {
    var m = el("div", "kurix-msg " + (kind === "user" ? "kurix-user" : "kurix-bot"));
    if (kind === "user") m.style.background = config.color;
    m.textContent = text;
    msgs.appendChild(m);
    scrollDown();
    return m;
  }

  function showTyping() {
    var t = el("div", "kurix-typing", "escribiendo…");
    msgs.appendChild(t);
    scrollDown();
    return t;
  }

  function scrollDown() {
    msgs.scrollTop = msgs.scrollHeight;
  }

  function escapeHtml(s) {
    var d = document.createElement("div");
    d.textContent = s;
    return d.innerHTML;
  }

  // ---- Networking ----
  function send() {
    var text = textarea.value.trim();
    if (!text || state.sending) return;

    addMessage("user", text);
    textarea.value = "";
    state.sending = true;
    sendBtn.disabled = true;

    var typing = showTyping();

    var body = { message: text };
    if (state.sessionId) body.sessionId = state.sessionId;

    fetch(config.chatUrl, {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-Api-Key": config.apiKey },
      body: JSON.stringify(body)
    })
      .then(function (res) {
        if (!res.ok) throw new Error("HTTP " + res.status);
        return res.json();
      })
      .then(function (data) {
        typing.remove();
        if (data.sessionId) {
          state.sessionId = data.sessionId;
          safeSet(STORAGE_KEY, data.sessionId);
        }
        addMessage("bot", data.reply || "");
        if (data.escalated && !state.escalated) {
          state.escalated = true;
          banner.classList.add("kurix-show");
        }
      })
      .catch(function (err) {
        typing.remove();
        addMessage("bot", "Disculpá, hubo un problema al enviar tu mensaje. Intentá de nuevo en un momento.");
        console.error("[Kurix]", err);
      })
      .finally(function () {
        state.sending = false;
        sendBtn.disabled = false;
        textarea.focus();
      });
  }

  // ---- Boot ----
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", build);
  } else {
    build();
  }
})();
