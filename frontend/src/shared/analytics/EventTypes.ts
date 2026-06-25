export enum EventType {
  Viewed = 'Viewed',
  Opened = 'Opened',
  Saved = 'Saved',
  Shared = 'Shared',
  Searched = 'Searched',
  Ignored = 'Ignored',
  Revisited = 'Revisited',
  FeedViewed = 'FeedViewed',
  Clicked = 'Clicked',
  Bookmarked = 'Bookmarked',
  CollectionOpened = 'CollectionOpened',
  SessionEnded = 'SessionEnded'
}

export interface AnalyticsEvent {
  eventType: EventType | string;
  entityId?: string;
  metadata?: Record<string, any>;
  timestamp: string;
}
