import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import path from 'path';

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      // The update toast (PwaUpdatePrompt) asks before reloading, so an open form is never lost
      registerType: 'prompt',
      injectRegister: false,
      includeAssets: ['favicon.svg', 'apple-touch-icon-180x180.png'],
      manifest: {
        id: '/',
        name: 'Movie Rater',
        short_name: 'Movie Rater',
        description:
          'Track the movies you watch together. Rate, review, and discover your shared cinematic journey.',
        start_url: '/dashboard',
        scope: '/',
        display: 'standalone',
        background_color: '#090b0c',
        theme_color: '#090b0c',
        categories: ['entertainment', 'lifestyle'],
        icons: [
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: 'maskable-icon-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,svg,png,woff2}'],
        // Serve the SPA shell for client-side routes, but never intercept API calls
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//],
        cleanupOutdatedCaches: true,
        runtimeCaching: [
          {
            // TMDB posters and backdrops are immutable per URL, so serve them from cache first
            urlPattern: ({ url }) => url.origin === 'https://image.tmdb.org',
            handler: 'CacheFirst',
            options: {
              cacheName: 'tmdb-images',
              expiration: { maxEntries: 300, maxAgeSeconds: 60 * 60 * 24 * 30, purgeOnQuotaError: true },
              cacheableResponse: { statuses: [0, 200] },
            },
          },
        ],
      },
    }),
  ],
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
