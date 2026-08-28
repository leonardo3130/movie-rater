import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname),
      '@src': path.resolve(__dirname, 'src'),
    },
  },
  server: {
    allowedHosts: [
      'movie-rater.leopo.dev' // Permette al server Vite del frontend di accettare richieste su questo dominio
    ],
    proxy: {
      '/api': {
        target: 'https://localhost:7283',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
