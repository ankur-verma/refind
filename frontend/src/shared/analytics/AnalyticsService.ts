import { AnalyticsEvent, EventType } from './EventTypes';
import { API_BASE_URL, request } from '../../api';

const BATCH_SIZE = 10;
const FLUSH_INTERVAL_MS = 5000;
const STORAGE_KEY = 'refind_analytics_queue';

class AnalyticsService {
  private queue: AnalyticsEvent[] = [];
  private flushTimer: NodeJS.Timeout | null = null;
  private isProcessing = false;

  constructor() {
    this.loadFromStorage();
    
    // Attempt to flush before unload
    window.addEventListener('beforeunload', () => {
      if (this.queue.length > 0) {
        this.saveToStorage();
        // Fire and forget using keepalive if possible, or just rely on storage for next session
        if (navigator.sendBeacon) {
          const token = localStorage.getItem('token');
          const headers = { 'Content-Type': 'application/json' };
          if (token) {
            // Cannot easily add Authorization header to sendBeacon, so relying on cookie or saving to storage
            // Just save to storage for now and let the next session pick it up
          }
        }
      }
    });

    // Start background flush interval
    this.startFlushTimer();
  }

  private loadFromStorage() {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        this.queue = JSON.parse(stored);
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch (e) {
      console.error('Failed to load analytics queue', e);
    }
  }

  private saveToStorage() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.queue));
    } catch (e) {
      console.error('Failed to save analytics queue', e);
    }
  }

  private startFlushTimer() {
    if (this.flushTimer) clearInterval(this.flushTimer);
    this.flushTimer = setInterval(() => {
      this.flush();
    }, FLUSH_INTERVAL_MS);
  }

  public track(eventType: EventType, entityId?: string, metadata?: Record<string, any>) {
    const event: AnalyticsEvent = {
      eventType,
      entityId,
      metadata,
      timestamp: new Date().toISOString()
    };

    this.queue.push(event);

    if (this.queue.length >= BATCH_SIZE) {
      this.flush();
    }
  }

  public async flush() {
    if (this.isProcessing || this.queue.length === 0) return;

    this.isProcessing = true;
    const originalEvents = [...this.queue];
    const eventsToSend = originalEvents.map(e => ({
      ContentItemId: e.entityId && e.entityId.length > 10 ? e.entityId : null,
      EventType: e.eventType,
      MetadataJson: JSON.stringify({ ...e.metadata, timestamp: e.timestamp })
    }));
    
    try {
      // Send to .NET Backend Ingestion API
      const response = await request('/behavior/events', {
        method: 'POST',
        body: JSON.stringify(eventsToSend)
      });

      if (response.success || response.status === 200 || response.status === 202) {
        // Success, remove sent events from queue
        this.queue = this.queue.filter(e => !originalEvents.includes(e));
      } else {
        throw new Error('Failed to send analytics');
      }
    } catch (err) {
      console.error('Analytics flush error', err);
      // Keep in queue, try again later
    } finally {
      this.isProcessing = false;
    }
  }
}

export const analytics = new AnalyticsService();
