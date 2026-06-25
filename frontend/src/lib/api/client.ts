import axios from 'axios';
import { v4 as uuidv4 } from 'uuid';

export const API_BASE_URL = 'http://localhost:5183/api/v1';

// Create a unique correlation ID for this session or generating a new one per request
let currentCorrelationId = uuidv4();

const client = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add a request interceptor
client.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }
    
    // Inject Correlation ID for telemetry and distributed tracing
    // We generate a new one for each request to track it through the system
    config.headers['X-Correlation-Id'] = uuidv4();

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add a response interceptor
client.interceptors.response.use(
  (response) => {
    return response.data;
  },
  (error) => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.dispatchEvent(new Event('auth-change'));
      // We will handle stopping SignalR in the auth event listener now
    }
    return Promise.reject(error);
  }
);

export default client;
