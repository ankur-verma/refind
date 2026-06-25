import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import AuthView from '../features/auth/AuthView';
import { useAuthStore } from '../stores/authStore';

export default function AuthPage() {
  const navigate = useNavigate();
  const { token } = useAuthStore();

  useEffect(() => {
    if (token) {
      // If already authenticated, redirect to dashboard
      navigate('/dashboard');
    }
  }, [token, navigate]);

  if (token) {
    return null;
  }

  const handleAuthSuccess = () => {
    navigate('/dashboard');
  };

  return <AuthView onAuthSuccess={handleAuthSuccess} />;
}
