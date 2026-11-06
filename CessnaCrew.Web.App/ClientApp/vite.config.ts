import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Backend-Adresse für den Proxy. Im Container teilen sich Frontend und Backend
// das Netzwerk des db-Services, darum ist das Backend unter localhost:5136 erreichbar.
const apiTarget = process.env.API_URL ?? 'http://localhost:5136'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    // Bind-Mounts von Windows liefern im Container keine Datei-Events, darum Polling.
    watch: process.env.VITE_USE_POLLING === 'true' ? { usePolling: true, interval: 300 } : undefined,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
})
