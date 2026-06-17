import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { QueryClient } from '@tanstack/react-query';

export const API_BASE_URL = 'http://localhost:5183/api/v1';
export const HUB_BASE_URL = 'http://localhost:5183/hubs/content';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

let connection = null;

export function setupSignalR() {
  const token = localStorage.getItem('token');
  if (!token) return;

  if (connection) {
    if (connection.state === 'Connected' || connection.state === 'Connecting') {
      return;
    }
    connection.stop();
  }

  connection = new HubConnectionBuilder()
    .withUrl(HUB_BASE_URL, {
      accessTokenFactory: () => localStorage.getItem('token'),
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();

  connection.on('ContentProcessed', (data) => {
    console.log('SignalR: ContentProcessed', data);
    // Invalidate feed query to trigger immediate refetch
    queryClient.invalidateQueries({ queryKey: ['feed'] });
  });

  connection.start().catch((err) => console.error('SignalR Connection Error: ', err));
}

export function stopSignalR() {
  if (connection) {
    connection.stop();
    connection = null;
  }
}

export async function request(endpoint, options = {}) {
  const token = localStorage.getItem('token');
  const headers = {
    'Content-Type': 'application/json',
    ...options.headers,
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      stopSignalR();
      window.dispatchEvent(new Event('auth-change'));
      throw new Error('Unauthorized');
    }

    if (response.status === 204) {
      return { success: true };
    }

    const json = await response.json();
    return json;
  } catch (error) {
    console.error(`API Error on ${endpoint}:`, error);
    return { success: false, errors: [error.message || 'Network error'] };
  }
}

export async function checkDbStatus() {
  try {
    const response = await fetch(`${API_BASE_URL}/health`);
    if (!response.ok) return { online: false, dbConnected: false };
    const data = await response.json();
    return { online: true, dbConnected: !!data.databaseConnected };
  } catch (err) {
    return { online: false, dbConnected: false };
  }
}
