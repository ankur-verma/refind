import { Star } from 'lucide-react';

export function generateAiMetadata(item: any) {
  const tags = (item.tags || []).map((t: string) => t.toLowerCase());
  const seed = item.id ? item.id.length : 10;
  
  let intent = "Content Discovery";
  let location = null;
  let categoryIcon = Star;
  let categoryLabel = "General";

  if (tags.some((t: string) => ['restaurant', 'food', 'dining', 'cafe'].includes(t))) {
    intent = "Place To Visit";
    location = "Gurgaon"; // Mock
    categoryLabel = tags.includes('family') ? 'Family Friendly' : 'Dining';
  } else if (tags.some((t: string) => ['vacation', 'travel', 'trip'].includes(t))) {
    intent = "Travel Inspiration";
    location = "Bali, Indonesia";
    categoryLabel = "Getaway";
  } else if (tags.some((t: string) => ['tech', 'shopping', 'gadget'].includes(t))) {
    intent = "Purchase Consideration";
    categoryLabel = "Tech & Gear";
  } else if (tags.some((t: string) => ['ai', 'code', 'design'].includes(t))) {
    intent = "Knowledge Acquisition";
    categoryLabel = "Deep Dive";
  }

  // Generate deterministic confidence score between 80-99
  const confidence = 80 + (seed % 20);

  return { intent, location, categoryLabel, categoryIcon, confidence };
}
