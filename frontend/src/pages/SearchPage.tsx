import SearchV2 from '../features/semantic-search/SearchV2';
import { useState } from 'react';
import ContentDetailModalV2 from '../features/content-feed/ContentDetailModalV2';

export default function SearchPage() {
  const [selectedContentId, setSelectedContentId] = useState<string | null>(null);

  return (
    <div className="h-full">
      <SearchV2 onSelectContent={setSelectedContentId} />
      {selectedContentId && (
        <ContentDetailModalV2 
          contentId={selectedContentId} 
          onClose={() => setSelectedContentId(null)}
        />
      )}
    </div>
  );
}
