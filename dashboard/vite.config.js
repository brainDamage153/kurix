import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// During dev, /api is proxied to the Kurix API to avoid CORS. Override the
// target with VITE_API_PROXY (e.g. http://localhost:5000) if needed.
const apiTarget = process.env.VITE_API_PROXY || "https://localhost:5001";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target: apiTarget, changeOrigin: true, secure: false }
    }
  }
});
