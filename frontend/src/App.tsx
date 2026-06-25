import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { setupSignalR, stopSignalR } from './api';
import { useAuthStore } from './stores/authStore';
import { Toaster } from './components/ui/toast';

// Layouts & Pages
import MainLayout from './layouts/MainLayout';
import AuthPage from './pages/AuthPage';
import DashboardPage from './pages/DashboardPage';
import ProfilePage from './pages/ProfilePage';
import IngestPage from './pages/IngestPage';
import DeclutterPage from './pages/DeclutterPage';
import SearchPage from './pages/SearchPage';
import DiscoverPage from './pages/DiscoverPage';
import CollectionsPage from './pages/CollectionsPage';
import CollectionDetail from './features/collections/CollectionDetail';
import SettingsPage from './pages/SettingsPage';
import ClipsPage from './pages/ClipsPage';

// Auth Guard Component
function RequireAuth({ children }: { children: JSX.Element }) {
  const { token } = useAuthStore();
  if (!token) {
    return <Navigate to="/login" replace />;
  }
  return children;
}

export default function App() {
  const { token, setAuth, logout } = useAuthStore();

  // SignalR Lifecycle
  useEffect(() => {
    if (token) {
      setupSignalR();
    } else {
      stopSignalR();
    }
  }, [token]);

  // Listen for interceptor 401s
  useEffect(() => {
    const handleAuthChange = () => {
      // If token was removed from localStorage, clear zustand store
      const localToken = localStorage.getItem('token');
      if (!localToken) {
        logout();
      }
    };
    window.addEventListener('auth-change', handleAuthChange);
    return () => window.removeEventListener('auth-change', handleAuthChange);
  }, [logout]);

  return (
    <BrowserRouter>
      <Toaster />
      <Routes>
        <Route path="/login" element={<AuthPage />} />
        
        {/* Protected Routes inside MainLayout */}
        <Route 
          path="/" 
          element={
            <RequireAuth>
              <MainLayout />
            </RequireAuth>
          }
        >
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="ingest" element={<IngestPage />} />
          <Route path="declutter" element={<DeclutterPage />} />
          <Route path="search" element={<SearchPage />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="discover" element={<DiscoverPage />} />
          <Route path="clips" element={<ClipsPage />} />
          <Route path="collections" element={<CollectionsPage />} />
          <Route path="collections/:id" element={<CollectionDetail />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
        
        {/* Fallback */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
