import InterestFeed from '../features/content-feed/InterestFeed';
import { useState } from 'react';
import ContentDetailModalV2 from '../features/content-feed/ContentDetailModalV2';

export default function DashboardPage() {
  const [selectedContentId, setSelectedContentId] = useState<string | null>(null);

  return (
    <div className="w-full">
      <InterestFeed onSelectContent={setSelectedContentId} />
      {selectedContentId && (
        <ContentDetailModalV2 
          contentId={selectedContentId} 
          onClose={() => setSelectedContentId(null)}
        />
      )}
    </div>
  );
}
