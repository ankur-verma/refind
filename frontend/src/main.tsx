import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { GoogleOAuthProvider } from '@react-oauth/google'
import { QueryClientProvider } from '@tanstack/react-query'
import { ErrorBoundary } from 'react-error-boundary'
import { queryClient } from './api'
import './index.css'
import App from './App'

const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID || 'dummy-client-id.apps.googleusercontent.com'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <GoogleOAuthProvider clientId={googleClientId}>
        <ErrorBoundary fallback={<div className="p-8 text-red-500">Something went wrong in the application. Please refresh.</div>}>
          <App />
        </ErrorBoundary>
      </GoogleOAuthProvider>
    </QueryClientProvider>
  </StrictMode>,
)
