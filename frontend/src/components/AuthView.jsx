import { useState, useEffect } from 'react';
import { Mail, Phone, Lock, AlertTriangle, Loader2, CheckCircle2, ShieldAlert } from 'lucide-react';
import { useGoogleLogin } from '@react-oauth/google';
import { request, checkDbStatus } from '../api';

export default function AuthView({ onAuthSuccess }) {
  const [isRegister, setIsRegister] = useState(false);
  const [authMethod, setAuthMethod] = useState('email'); // 'email' or 'phone'
  
  // Email Form State
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isEmailVerification, setIsEmailVerification] = useState(false);
  const [emailCode, setEmailCode] = useState('');
  
  // Phone Form State
  const [phoneNumber, setPhoneNumber] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [verificationSent, setVerificationSent] = useState(false);
  const [countdown, setCountdown] = useState(0);

  // OAuth Provider Setup
  const googleClientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
  const metaAppId = import.meta.env.VITE_META_APP_ID;
  const twitterClientId = import.meta.env.VITE_TWITTER_CLIENT_ID;
  const linkedinClientId = import.meta.env.VITE_LINKEDIN_CLIENT_ID;
  // DB Connectivity State
  const [dbState, setDbState] = useState({ online: false, dbConnected: false, checking: true });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Check database connectivity on mount
  useEffect(() => {
    const checkDb = async () => {
      const status = await checkDbStatus();
      setDbState({
        online: status.online,
        dbConnected: status.dbConnected,
        checking: false
      });
    };
    checkDb();
  }, []);

  // OTP Timer countdown
  useEffect(() => {
    if (countdown > 0) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000);
      return () => clearTimeout(timer);
    }
  }, [countdown]);

  // Form validations
  const validateEmailForm = () => {
    setError('');
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email.trim())) {
      setError('Please enter a valid email address.');
      return false;
    }
    if (password.length < 8) {
      setError('Password must be at least 8 characters long (backend restriction).');
      return false;
    }
    // Check uppercase and digit (backend rules)
    if (!/[A-Z]/.test(password) || !/[0-9]/.test(password)) {
      setError('Password must contain at least one uppercase letter and one digit.');
      return false;
    }
    if (isRegister && password !== confirmPassword) {
      setError('Passwords do not match.');
      return false;
    }
    return true;
  };

  const validatePhoneForm = () => {
    setError('');
    // Strip non-digits and check length
    const digits = phoneNumber.replace(/\D/g, '');
    if (digits.length < 10 || digits.length > 15) {
      setError('Please enter a valid phone number (at least 10 digits).');
      return false;
    }
    return true;
  };

  const handleEmailSubmit = async (e) => {
    e.preventDefault();
    if (!validateEmailForm()) return;
    
    setLoading(true);
    setError('');

    // If Database is connected, hit the real backend
    if (dbState.online && dbState.dbConnected) {
      const endpoint = isRegister ? '/auth/register' : '/auth/login';
      const payload = isRegister 
        ? { email, password, confirmPassword }
        : { email, password };
        
      const res = await request(endpoint, {
        method: 'POST',
        body: JSON.stringify(payload),
      });
      
      setLoading(false);
      if (res.success && res.data) {
        if (res.data.requiresVerification) {
          setIsEmailVerification(true);
          setError(''); // clear errors, show OTP UI
        } else {
          saveSession(res.data);
        }
      } else {
        setError(res.errors?.[0] || 'Authentication failed. Please check credentials.');
      }
    } else {
      // Offline Demo Mode: Simulated Login
      setTimeout(() => {
        setLoading(false);
        simulateLocalLogin(email, isRegister ? 'Premium' : 'Free');
      }, 1000);
    }
  };

  const handleVerifyEmailOTP = async (e) => {
    e.preventDefault();
    if (emailCode.length !== 6) {
      setError('Please enter a 6-digit code.');
      return;
    }

    setLoading(true);
    setError('');

    if (dbState.online && dbState.dbConnected) {
      const res = await request('/auth/verify-email', {
        method: 'POST',
        body: JSON.stringify({ email, code: emailCode })
      });

      setLoading(false);
      if (res.success && res.data) {
        saveSession(res.data);
      } else {
        setError(res.errors?.[0] || 'Invalid or expired verification code.');
      }
    }
  };

  const handleSendSMS = async (e) => {
    e.preventDefault();
    if (!validatePhoneForm()) return;
    
    setLoading(true);
    setError('');

    if (dbState.online && dbState.dbConnected) {
      const res = await request('/auth/phone/send-code', {
        method: 'POST',
        body: JSON.stringify({ phoneNumber })
      });

      setLoading(false);
      if (res.success) {
        setVerificationSent(true);
        setCountdown(59);
      } else {
        setError(res.errors?.[0] || 'Failed to send SMS code.');
      }
    } else {
      // Simulate OTP generation
      setTimeout(() => {
        setLoading(false);
        setVerificationSent(true);
        setCountdown(59);
      }, 1200);
    }
  };

  const handleVerifyOTP = async (e) => {
    e.preventDefault();
    if (verificationCode.length !== 6) {
      setError('Please enter a 6-digit code.');
      return;
    }

    setLoading(true);
    setError('');

    // If DB is connected, register/login virtual account in DB!
    if (dbState.online && dbState.dbConnected) {
      const res = await request('/auth/phone/verify', {
        method: 'POST',
        body: JSON.stringify({ phoneNumber, code: verificationCode }),
      });

      setLoading(false);
      if (res.success && res.data) {
        saveSession(res.data);
      } else {
        setError(res.errors?.[0] || 'Invalid or expired verification code.');
      }
    } else {
      // Offline Demo Mode: Log in locally
      setTimeout(() => {
        setLoading(false);
        const normalizedPhone = phoneNumber.replace(/\D/g, '');
        const virtualEmail = `phone-${normalizedPhone}@phone.refind`;
        simulateLocalLogin(virtualEmail, 'Free');
      }, 800);
    }
  };

  const handleOAuthLogin = (providerName) => {
    setError('');
    
    // Google
    if (providerName === 'google') {
      if (!googleClientId || googleClientId === 'dummy-client-id.apps.googleusercontent.com') {
        setError('Google OAuth is not configured. Missing VITE_GOOGLE_CLIENT_ID in environment.');
        return;
      }
      handleGoogleLogin();
      return;
    }

    // Meta
    if (providerName === 'meta') {
      if (!metaAppId) {
        setError('Meta OAuth is not configured. Missing VITE_META_APP_ID in environment.');
        return;
      }
      // Trigger Meta SDK...
      return;
    }

    // Twitter
    if (providerName === 'twitter') {
      if (!twitterClientId) {
        setError('Twitter/X OAuth is not configured. Missing VITE_TWITTER_CLIENT_ID in environment.');
        return;
      }
      // Trigger Twitter SDK...
      return;
    }

    // LinkedIn
    if (providerName === 'linkedin') {
      if (!linkedinClientId) {
        setError('LinkedIn OAuth is not configured. Missing VITE_LINKEDIN_CLIENT_ID in environment.');
        return;
      }
      // Trigger LinkedIn SDK...
      return;
    }
  };

  const handleGoogleLogin = useGoogleLogin({
    onSuccess: async (credentialResponse) => {
      setLoading(true);
      const res = await request('/auth/oauth', {
        method: 'POST',
        body: JSON.stringify({ 
          provider: 1, // 1 = Google in AuthProvider enum
          idToken: credentialResponse.access_token // Note: In a true OIDC flow, use standard <GoogleLogin> for an id_token, or validate the access_token in the backend.
        })
      });
      setLoading(false);
      if (res.success && res.data) {
        saveSession(res.data);
      } else {
        setError(res.errors?.[0] || 'Google login failed on backend validation.');
      }
    },
    onError: () => {
      setError('Google login popup was closed or failed.');
    }
  });

  const simulateLocalLogin = (emailAddress, tier) => {
    const mockData = {
      accessToken: 'mock-jwt-token-offline-mode',
      email: emailAddress,
      userId: '00000000-0000-0000-0000-000000000000',
      tier: tier === 'Premium' ? 1 : 0
    };
    saveSession(mockData);
  };

  const saveSession = (data) => {
    localStorage.setItem('token', data.accessToken);
    localStorage.setItem('user', JSON.stringify({
      email: data.email,
      userId: data.userId,
      tier: data.tier
    }));
    onAuthSuccess();
  };

  return (
    <div className="auth-container">
      {/* DB Connection Banner */}
      {!dbState.checking && (!dbState.online || !dbState.dbConnected) && (
        <div className="db-offline-banner">
          <ShieldAlert size={18} />
          <span>
            {!dbState.online 
              ? 'Cortex Monolith Server is offline. Running Refind in Local Demo Mode.'
              : 'PostgreSQL Database is not connected (installing via brew). Running in Local Demo Mode.'}
          </span>
        </div>
      )}

      <div className="auth-card glass">
        <div className="auth-header">
          <h2>Refind Cognitive Mind</h2>
          <p>Organize URLs, pictures, videos into structured memory banks</p>
        </div>

        {/* Auth Method Selector */}
        <div className="auth-tabs">
          <button 
            type="button" 
            className={`auth-tab ${authMethod === 'email' ? 'active' : ''}`}
            onClick={() => { setAuthMethod('email'); setError(''); setVerificationSent(false); }}
          >
            <Mail size={16} /> Email Credentials
          </button>
          <button 
            type="button" 
            className={`auth-tab ${authMethod === 'phone' ? 'active' : ''}`}
            onClick={() => { setAuthMethod('phone'); setError(''); setVerificationSent(false); }}
          >
            <Phone size={16} /> Phone SMS Code
          </button>
        </div>
        
        {error && (
          <div className="auth-error animate-fade-in">
            <AlertTriangle size={18} />
            <span>{error}</span>
          </div>
        )}
        
        {authMethod === 'email' ? (
          !isEmailVerification ? (
            <form onSubmit={handleEmailSubmit} className="auth-form">
              <div className="form-group">
                <label>Email Address</label>
                <div className="input-wrapper">
                  <Mail size={18} className="input-icon" />
                  <input
                    type="email"
                    placeholder="name@domain.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                  />
                </div>
              </div>
              
              <div className="form-group">
                <label>Password</label>
                <div className="input-wrapper">
                  <Lock size={18} className="input-icon" />
                  <input
                    type="password"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                </div>
              </div>
              
              {isRegister && (
                <div className="form-group animate-slide-down">
                  <label>Confirm Password</label>
                  <div className="input-wrapper">
                    <Lock size={18} className="input-icon" />
                    <input
                      type="password"
                      placeholder="••••••••"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      required
                    />
                  </div>
                </div>
              )}
              
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? (
                  <span className="btn-loading">
                    <Loader2 size={18} className="spin" /> Contacting Cortex Server...
                  </span>
                ) : (
                  isRegister ? 'Register Virtual Account' : 'Authenticate Credentials'
                )}
              </button>
            </form>
          ) : (
            <form onSubmit={handleVerifyEmailOTP} className="auth-form animate-slide-down">
              <div className="form-group animate-pulse">
                <span className="otp-sent-label">Verification Code sent to {email}</span>
              </div>
              <div className="form-group">
                <label>6-Digit Verification Code</label>
                <div className="input-wrapper">
                  <Lock size={18} className="input-icon" />
                  <input
                    type="text"
                    maxLength="6"
                    placeholder="••••••"
                    value={emailCode}
                    onChange={(e) => setEmailCode(e.target.value.replace(/\D/g, ''))}
                    required
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? <Loader2 size={18} className="spin" /> : 'Verify Code & Sign In'}
              </button>
              <div className="otp-countdown-row">
                <button type="button" className="btn-link" onClick={() => setIsEmailVerification(false)}>
                  Back to login
                </button>
              </div>
            </form>
          )
        ) : (
          /* Phone OTP Auth Flow */
          !verificationSent ? (
            <form onSubmit={handleSendSMS} className="auth-form animate-fade-in">
              <div className="form-group">
                <label>Phone Number (with Country Code)</label>
                <div className="input-wrapper">
                  <Phone size={18} className="input-icon" />
                  <input
                    type="tel"
                    placeholder="+1 (555) 123-4567"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    required
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? <Loader2 size={18} className="spin" /> : 'Send Verification OTP'}
              </button>
            </form>
          ) : (
            <form onSubmit={handleVerifyOTP} className="auth-form animate-slide-down">
              <div className="form-group animate-pulse">
                <span className="otp-sent-label">Verification OTP sent to {phoneNumber}</span>
              </div>
              <div className="form-group">
                <label>6-Digit Verification Code</label>
                <div className="input-wrapper">
                  <Lock size={18} className="input-icon" />
                  <input
                    type="text"
                    maxLength="6"
                    placeholder="••••••"
                    value={verificationCode}
                    onChange={(e) => setVerificationCode(e.target.value.replace(/\D/g, ''))}
                    required
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? <Loader2 size={18} className="spin" /> : 'Verify Code & Sign In'}
              </button>
              <div className="otp-countdown-row">
                {countdown > 0 ? (
                  <span>Resend code in {countdown}s</span>
                ) : (
                  <button type="button" className="btn-link" onClick={handleSendSMS}>Resend SMS Code</button>
                )}
              </div>
            </form>
          )
        )}
        
        {authMethod === 'email' && (
          <div className="auth-footer">
            <span>{isRegister ? 'Already have an account?' : "Don't have an account?"}</span>
            <button 
              type="button" 
              className="btn-link"
              onClick={() => {
                setIsRegister(!isRegister);
                setError('');
              }}
            >
              {isRegister ? 'Sign In' : 'Sign Up'}
            </button>
          </div>
        )}

        <div className="oauth-divider">
          <span>Or sign in with OAuth Providers</span>
        </div>

        {/* Premium OAuth Login Grid */}
        <div className="oauth-grid">
          {/* Google */}
          <button 
            type="button" 
            className="oauth-btn google"
            onClick={() => handleOAuthLogin('google')}
            title="Google OAuth"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" xmlns="http://www.w3.org/2000/svg">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
              <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" fill="#FBBC05"/>
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
            </svg>
            <span>Google</span>
          </button>

          {/* Meta */}
          <button 
            type="button" 
            className="oauth-btn meta"
            onClick={() => handleOAuthLogin('meta')}
            title="Meta OAuth"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
              <path d="M15.44 3.5c-1.57 0-3.15.58-4.32 1.74l-.99.98c-1.17-1.16-2.75-1.74-4.32-1.74-3.37 0-6.11 2.74-6.11 6.11 0 1.63.64 3.2 1.79 4.35 1.15 1.15 2.7 1.76 4.32 1.76 1.57 0 3.15-.58 4.32-1.74l.99-.98c1.17 1.16 2.75 1.74 4.32 1.74 1.62 0 3.17-.61 4.32-1.76 1.15-1.15 1.79-2.72 1.79-4.35 0-3.37-2.74-6.11-6.11-6.11zm0 10.42c-.93 0-1.85-.35-2.55-1.05l-1.42-1.42c-.22-.22-.59-.22-.81 0l-1.42 1.42c-.7.7-1.62 1.05-2.55 1.05-.96 0-1.87-.37-2.55-1.05C3.47 11.23 3.1 10.32 3.1 9.36c0-2 .13-3.66 2.61-3.66.93 0 1.85.35 2.55 1.05l1.42 1.42c.22.22.59.22.81 0l1.42-1.42c.7-.7 1.62-1.05 2.55-1.05.96 0 1.87.37 2.55 1.05.68.68 1.05 1.59 1.05 2.55 0 .96-.37 1.87-1.05 2.55-.68.68-1.59 1.05-2.55 1.05z"/>
            </svg>
            <span>Meta</span>
          </button>

          {/* Twitter / X */}
          <button 
            type="button" 
            className="oauth-btn twitter"
            onClick={() => handleOAuthLogin('twitter')}
            title="Twitter OAuth"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
              <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
            </svg>
            <span>X</span>
          </button>

          {/* LinkedIn */}
          <button 
            type="button" 
            className="oauth-btn linkedin"
            onClick={() => handleOAuthLogin('linkedin')}
            title="LinkedIn OAuth"
          >
            <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
              <path d="M19 0h-14c-2.761 0-5 2.239-5 5v14c0 2.761 2.239 5 5 5h14c2.762 0 5-2.239 5-5v-14c0-2.761-2.238-5-5-5zm-11 19h-3v-11h3v11zm-1.5-12.268c-.966 0-1.75-.779-1.75-1.75s.784-1.75 1.75-1.75 1.75.779 1.75 1.75-.784 1.75-1.75 1.75zm13.5 12.268h-3v-5.604c0-3.368-4-3.113-4 0v5.604h-3v-11h3v1.765c1.396-2.586 7-2.777 7 2.476v6.759z"/>
            </svg>
            <span>LinkedIn</span>
          </button>
        </div>
      </div>

      {/* Removed the Simulated OAuth Modal Overlay */}
    </div>
  );
}
