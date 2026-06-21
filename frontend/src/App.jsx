import { useState, useEffect } from 'react';
import AuthView from './components/AuthView';
import { setupSignalR, stopSignalR } from './api';
import AppLayoutV2 from './v2/AppLayoutV2';

export default function App() {
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [user, setUser] = useState(null);

  useEffect(() => {
    // Parse user details from storage if token exists
    const savedUser = localStorage.getItem('user');
    if (token && savedUser) {
      try {
        setUser(JSON.parse(savedUser));
      } catch (_) {
        setUser({ email: 'user@example.com', tier: 0 });
      }
    } else {
      setUser(null);
      stopSignalR();
    }
  }, [token]);

  useEffect(() => {
    if (token) {
      setupSignalR();
    } else {
      stopSignalR();
    }
  }, [token]);

  // Listen for API helper firing logouts
  useEffect(() => {
    const handleAuthChange = () => {
      setToken(localStorage.getItem('token'));
    };
    window.addEventListener('auth-change', handleAuthChange);
    return () => window.removeEventListener('auth-change', handleAuthChange);
  }, []);

  const handleAuthSuccess = () => {
    setToken(localStorage.getItem('token'));
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  // If not authenticated, force AuthView
  if (!token) {
    return <AuthView onAuthSuccess={handleAuthSuccess} />;
  }

  return <AppLayoutV2 user={user} handleLogout={handleLogout} />;
}
