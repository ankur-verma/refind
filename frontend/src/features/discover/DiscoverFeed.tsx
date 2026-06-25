import { useState } from 'react';
import FeedSection from '../content-feed/components/FeedSection';
import FeedCard from '../content-feed/components/FeedCard';
import ContentDetailModalV2 from '../content-feed/ContentDetailModalV2';
import { request } from '../../api';

// Realistic mock discovery data tailored to simulate an AI Recommendation Engine
const DISCOVERY_MOCKS = {
  places: [
    { id: 'disc-p1', platformType: 1, energyLevel: 2, tags: ['travel', 'trip', 'vacation', 'place'], title: 'The Hidden Hot Springs of Hokkaido', summary: 'A complete guide to finding the most remote and serene hot springs in Northern Japan.', url: 'https://example.com/hokkaido', thumbnailUrl: 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=800&auto=format&fit=crop' },
    { id: 'disc-p2', platformType: 1, energyLevel: 1, tags: ['travel', 'trip', 'place'], title: 'Kyoto Off The Beaten Path', summary: 'Skip the crowds and explore the hidden temples of Eastern Kyoto.', url: 'https://example.com/kyoto', thumbnailUrl: 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=800&auto=format&fit=crop' },
  ],
  products: [
    { id: 'disc-pr1', platformType: 1, energyLevel: 1, tags: ['shopping', 'tech', 'gadget'], title: 'Sony WH-1000XM6 Rumors', summary: 'What to expect from the next generation of industry-leading noise cancellation.', url: 'https://example.com/sony', thumbnailUrl: 'https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?q=80&w=800&auto=format&fit=crop' },
    { id: 'disc-pr2', platformType: 0, energyLevel: 1, tags: ['shopping', 'tech', 'gadget'], title: 'Mechanical Keyboard Build Guide', summary: 'Building the ultimate thocky keyboard under $200.', url: 'https://example.com/keyboard', thumbnailUrl: 'https://images.unsplash.com/photo-1595225476474-87563907a212?q=80&w=800&auto=format&fit=crop' },
  ],
  restaurants: [
    { id: 'disc-r1', platformType: 1, energyLevel: 2, tags: ['restaurant', 'food', 'dining'], title: 'Top 5 Omakase in Manhattan', summary: 'The ultimate tasting menu experiences ranked by value and authenticity.', url: 'https://example.com/omakase', thumbnailUrl: 'https://images.unsplash.com/photo-1553621042-f6e147245754?q=80&w=800&auto=format&fit=crop' },
    { id: 'disc-r2', platformType: 2, energyLevel: 0, tags: ['restaurant', 'food', 'cafe', 'family'], title: 'Best Brunch Spots with Kids', summary: 'Family friendly cafes that still serve incredible coffee and pastries.', url: 'https://example.com/brunch', thumbnailUrl: 'https://images.unsplash.com/photo-1509042239860-f550ce710b93?q=80&w=800&auto=format&fit=crop' },
  ],
  videos: [
    { id: 'disc-v1', platformType: 0, energyLevel: 2, tags: ['ai', 'code', 'design'], title: 'Building a Neural Network from Scratch', summary: 'A deep dive into the math and implementation of backpropagation.', url: 'https://example.com/nn', thumbnailUrl: 'https://images.unsplash.com/photo-1620712943543-bcc4688e7485?q=80&w=800&auto=format&fit=crop' },
    { id: 'disc-v2', platformType: 0, energyLevel: 1, tags: ['code', 'architecture'], title: 'System Design Interview Prep', summary: 'How to design a scalable Twitter clone.', url: 'https://example.com/system', thumbnailUrl: 'https://images.unsplash.com/photo-1555949963-aa79dcee981c?q=80&w=800&auto=format&fit=crop' },
  ]
};

export default function DiscoverFeed() {
  const [selectedContentId, setSelectedContentId] = useState<string | null>(null);

  const handleSaveToBrain = async (e: React.MouseEvent, item: any) => {
    e.stopPropagation();
    console.log("Saving to brain:", item.url);
    try {
      await request('/content', { method: 'POST', body: JSON.stringify({ url: item.url }) });
      // Show some temporary success toast here if desired
    } catch (err) {
      console.error("Failed to save", err);
    }
  };

  return (
    <div className="pb-20 relative space-y-12 pt-6">
      <FeedSection title="Trending Places" subtitle="Destinations similar to your saved trips.">
        {DISCOVERY_MOCKS.places.map(item => (
          <FeedCard 
            key={item.id} 
            item={item} 
            isDiscovery={true} 
            onClick={setSelectedContentId} 
            onSaveToBrain={(e) => handleSaveToBrain(e, item)}
          />
        ))}
      </FeedSection>

      <FeedSection title="Gear & Products" subtitle="Based on your recent Tech & Gadget saves.">
        {DISCOVERY_MOCKS.products.map(item => (
          <FeedCard 
            key={item.id} 
            item={item} 
            isDiscovery={true} 
            onClick={setSelectedContentId} 
            onSaveToBrain={(e) => handleSaveToBrain(e, item)}
          />
        ))}
      </FeedSection>

      <FeedSection title="Restaurants to Try" subtitle="Highly rated spots in your area.">
        {DISCOVERY_MOCKS.restaurants.map(item => (
          <FeedCard 
            key={item.id} 
            item={item} 
            isDiscovery={true} 
            onClick={setSelectedContentId} 
            onSaveToBrain={(e) => handleSaveToBrain(e, item)}
          />
        ))}
      </FeedSection>

      <FeedSection title="Deep Dives" subtitle="Long-form videos matching your knowledge graph.">
        {DISCOVERY_MOCKS.videos.map(item => (
          <FeedCard 
            key={item.id} 
            item={item} 
            isDiscovery={true} 
            onClick={setSelectedContentId} 
            onSaveToBrain={(e) => handleSaveToBrain(e, item)}
          />
        ))}
      </FeedSection>

      {/* Note: In Discover tab, opening a modal might just open the URL natively or show a simplified modal since it's not saved yet. 
          For now, we just pass the ID. The ContentDetailModalV2 will try to fetch it, which will fail for mock IDs. 
          We handle this by just logging or opening externally in a real scenario. */}
      {selectedContentId && (
        <ContentDetailModalV2 
          contentId={selectedContentId} 
          onClose={() => setSelectedContentId(null)}
        />
      )}
    </div>
  );
}
