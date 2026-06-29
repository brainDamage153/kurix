# Kurix Widget

Widget de chat embebible, **vanilla JS, sin dependencias ni build step**.

## Uso (2 líneas)

```html
<script src="https://cdn.kurix.io/widget.js" data-tenant="API_KEY"></script>
```

La burbuja de chat aparece abajo a la derecha. Al abrirla, el usuario conversa
con el motor de Kurix del tenant correspondiente a la API key.

## Atributos

| Atributo | Requerido | Default | Descripción |
|----------|-----------|---------|-------------|
| `data-tenant` | ✅ | — | API key del tenant (se envía como header `X-Api-Key`). |
| `data-api-url` | — | origen del `src` del script | Base URL de la API de Kurix. |
| `data-title` | — | `Asistente` | Título del header. |
| `data-color` | — | `#4f46e5` | Color de acento. |
| `data-greeting` | — | saludo por defecto | Mensaje inicial del bot. |

## Comportamiento

- Mantiene el `sessionId` en `localStorage` (por API key), así la conversación
  persiste entre recargas.
- Llama a `POST {api-url}/api/chat` con `{ sessionId, message }` y el header
  `X-Api-Key`.
- Muestra un **indicador visual de escalamiento**: cuando la respuesta marca
  `escalated`, aparece un banner avisando que la conversación fue derivada a un
  agente humano.
- Maneja errores de red con un mensaje de respaldo, sin romper la UI.
- Responsive: en pantallas chicas el panel ocupa casi todo el viewport.

## Probar localmente

1. Levantá la API (`dotnet run --project src/Kurix.Api`). El seeder de
   desarrollo imprime una API key de demo en la consola.
2. Serví esta carpeta con cualquier static server, por ejemplo:
   ```bash
   npx serve widget        # o: python3 -m http.server --directory widget 8080
   ```
3. Abrí `demo.html`, pegá la API key y la URL de la API, y cargá el widget.

> En producción, `widget.js` se publica en un CDN (`https://cdn.kurix.io/widget.js`).
> Para reducir el tamaño se puede minificar; el archivo fuente ya es pequeño y
> autónomo.
