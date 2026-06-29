# Kurix Dashboard

Panel de control del Módulo 1, en **React + Vite**.

## Funcionalidad

- **Login** con JWT (email + password contra `/api/auth/login`).
- **Métricas**: total de conversaciones, tasa de escalamiento, tokens (in/out) y
  costo estimado, preguntas frecuentes.
- **Conversaciones**: lista con estado y detalle con el hilo completo
  (usuario / asistente / herramientas).
- **Base de conocimiento**: subir documentos (archivo `.txt`/`.md` o texto
  pegado) y eliminarlos.
- **Configuración**: persona del bot, mensaje de fallback, webhook de
  escalamiento y tools habilitadas.

Todas las llamadas envían el JWT en `Authorization: Bearer …`; la API scopa cada
query al tenant del token.

## Desarrollo

```bash
cd dashboard
npm install
npm run dev        # http://localhost:5173
```

En dev, Vite hace proxy de `/api` hacia la API de Kurix. El target por defecto es
`https://localhost:5001`; sobrescribilo con la variable de entorno
`VITE_API_PROXY` (por ejemplo `VITE_API_PROXY=http://localhost:5000 npm run dev`).

Para apuntar a una API en otro origen sin proxy, definí `VITE_API_URL` en un
archivo `.env`.

Credenciales de demo (del seeder de desarrollo de la API): `demo@kurix.io` /
`Demo1234!`.

## Build de producción

```bash
npm run build      # genera dist/
npm run preview    # sirve el build localmente
```
