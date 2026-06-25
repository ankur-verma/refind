import { useCallback } from 'react';
import { analytics } from './AnalyticsService';
import { EventType } from './EventTypes';

export function useTrackEvent() {
  const track = useCallback((eventType: EventType, entityId?: string, metadata?: Record<string, any>) => {
    analytics.track(eventType, entityId, metadata);
  }, []);

  return { track };
}
