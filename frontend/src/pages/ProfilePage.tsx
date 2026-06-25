import ProfileDashboard from '../features/profile/ProfileDashboard';
import { useState } from 'react';
import ContentDetailModalV2 from '../features/content-feed/ContentDetailModalV2';
import { useAuthStore } from '../stores/authStore';

export default function ProfilePage() {
  const { user } = useAuthStore();
  const [selectedContentId, setSelectedContentId] = useState<string | null>(null);

  return (
    <div className="min-h-full">
      <ProfileDashboard user={user} onSelectContent={setSelectedContentId} />
      {selectedContentId && (
        <ContentDetailModalV2 
          contentId={selectedContentId} 
          onClose={() => setSelectedContentId(null)}
        />
      )}
    </div>
  );
}
