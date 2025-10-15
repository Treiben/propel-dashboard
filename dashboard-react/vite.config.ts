import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => ({
	plugins: [react()],

	// Development server configuration
	server: {
		host: '0.0.0.0',
		port: 5173,
		proxy: mode === 'development' ? {
			'/api': {
				target: process.env.VITE_API_URL || 'http://propel-dashboard-api:8080',
				changeOrigin: true,
				secure: false,
			}
		} : undefined
	},

	preview: {
		host: '0.0.0.0',
		port: 8080
	},

	// Production build configuration
	build: {
		outDir: 'dist',
		emptyOutDir: true,
		sourcemap: false,
		rollupOptions: {
			output: {
				manualChunks: {
					vendor: ['react', 'react-dom'],
					utils: ['axios', 'lucide-react']
				}
			}
		}
	}
}));