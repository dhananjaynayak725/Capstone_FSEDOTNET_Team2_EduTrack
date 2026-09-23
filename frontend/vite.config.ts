import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The dev server proxies /api to the ASP.NET Core API, so the browser sees one origin (no CORS setup needed).
// Change the target if your API runs on a different port (see backend/EduTrack.API/Properties/launchSettings.json).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
});
