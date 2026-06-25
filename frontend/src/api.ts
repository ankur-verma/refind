import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { QueryClient } from '@tanstack/react-query';
import client from './lib/api/client';

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
    queryClient.invalidateQueries({ queryKey: ['interestFeed'] });
    
    // Dispatch custom event for UI tracking
    window.dispatchEvent(new CustomEvent('content-processed', { detail: data }));
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
  try {
    const response = await client({
      url: endpoint,
      method: options.method || 'GET',
      data: options.body ? JSON.parse(options.body) : undefined,
      ...options
    });
    
    // Some old fetch APIs returned 204 with success manually, Axios throws on 400+, so we just return the data or success
    return response || { success: true };
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
