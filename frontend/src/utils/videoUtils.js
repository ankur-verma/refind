export function createFragmentUrl(url, startSec, endSec) {
  // Try to parse YouTube URL
  let videoId = null;
  if (url.includes('youtube.com/watch')) {
    const urlObj = new URL(url);
    videoId = urlObj.searchParams.get('v');
  } else if (url.includes('youtube.com/shorts/')) {
    videoId = url.split('shorts/')[1].split('?')[0];
  } else if (url.includes('youtu.be/')) {
    videoId = url.split('youtu.be/')[1].split('?')[0];
  }
  
  const start = Math.max(0, startSec);
  const end = Math.max(start, endSec);
  
  if (videoId) {
    return `https://www.youtube.com/embed/${videoId}?start=${start}&end=${end}&autoplay=1&mute=1&controls=0&modestbranding=1&rel=0&loop=1&playlist=${videoId}`;
  }

  // Fallback for native media
  return `${url}#t=${start},${end}`;
}

// Simple tag similarity (placeholder for future client-side grouping)
export function shareTags(tagsA, tagsB) {
  const setA = new Set(tagsA.map(t => t.toLowerCase()));
  const setB = new Set(tagsB.map(t => t.toLowerCase()));
  let count = 0;
  setA.forEach(tag => {
    if (setB.has(tag)) count++;
  });
  return count;
}
