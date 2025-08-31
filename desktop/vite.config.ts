import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import electron from 'vite-plugin-electron/simple'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  // Use relative paths so Electron can load built files via file:// protocol
  base: './',
  resolve: {
    dedupe: ['react', 'react-dom'],
  },
  plugins: [
    react(),
    tailwindcss(),
    electron({
      main: {
        entry: 'electron/main.ts',
      },
      preload: {
        input: {
          preload: 'electron/preload.ts',
        },
        vite: {
          build: {
            minify: false,
            target: 'es2022',
            rollupOptions: {
              output: {
                format: 'esm',
              },
            },
          },
          resolve: {
            conditions: ['node'],
          },
          optimizeDeps: {
            exclude: ['electron', 'electron-store'],
          },
        },
      },
    }),
  ],
})
