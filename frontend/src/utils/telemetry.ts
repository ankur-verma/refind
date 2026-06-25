import { request } from '../api';

/**
 * Track a user interaction for AI profiling and telemetry.
 * 
 * @param {string|null} contentItemId - The ID of the content item, if applicable.
 * @param {string} interactionType - The type of interaction (e.g., 'View', 'Save', 'Search', 'Click').
 * @param {number} durationSeconds - The duration of the interaction in seconds.
 * @param {string|null} searchQuery - The search query, if applicable.
 */
export async function trackInteraction({
  contentItemId = null,
  interactionType,
  durationSeconds = 0,
  searchQuery = null
}) {
  try {
    const payload = {
      contentItemId,
      interactionType,
      durationSeconds,
      searchQuery
    };
    
    await request('/interaction/track', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
  } catch (error) {
    console.error('Failed to track interaction:', error);
  }
}
