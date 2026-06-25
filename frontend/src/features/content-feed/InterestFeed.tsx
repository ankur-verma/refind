import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { request } from '../../api';
import FeedSection from './components/FeedSection';
import FeedCard from './components/FeedCard';
import HeroCard from './components/HeroCard';
import { Skeleton } from '../../components/ui/skeleton';
import { Zap } from 'lucide-react';

export default function InterestFeed({ onSelectContent }: { onSelectContent: (id: string) => void }) {
  // Fetch a larger page size to ensure we have enough data to categorize on the frontend
  const fetchFeed = async () => {
    return await request('/content/feed?Page=1&PageSize=100');
  };

  const { data, isLoading, error } = useQuery({
    queryKey: ['interestFeed'],
    queryFn: fetchFeed,
  });

  const feed = data?.items || [];

  // --- Dynamic Frontend Categorization Logic ---
  // In a future phase, this logic should be moved to a dedicated backend endpoint.
  const categorized = useMemo(() => {
    // Only show items that are fully AI processed (status === 1 -> Ready)
    const items = feed.filter(i => i.status === 1);

    return {
      hero: items.length > 0 ? items[0] : null,
      continueExploring: items, 
      recentlySaved: [...items].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 10),
    };
  }, [feed]);

  if (isLoading) {
    return (
      <div className="flex flex-col gap-8 p-6 animate-pulse">
        <Skeleton className="w-full h-[50vh] rounded-2xl" />
        <div className="space-y-4">
          <Skeleton className="h-8 w-48" />
          <div className="flex gap-4 overflow-hidden">
            {[1, 2, 3, 4].map(i => <Skeleton key={i} className="w-64 h-96 rounded-2xl flex-none" />)}
          </div>
        </div>
      </div>
    );
  }

  if (error || feed.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-[80vh] text-muted-foreground space-y-4">
        <Zap size={48} className="text-primary opacity-50" />
        <p className="text-xl font-medium">Your Interest Feed is empty.</p>
        <p className="text-sm">Save some links to start generating your feed.</p>
      </div>
    );
  }

  return (
    <div className="pb-20 relative">
      <HeroCard item={categorized.hero} onClick={onSelectContent} />

      <div className="-mt-10 relative z-10">
        <FeedSection title="Continue Exploring" subtitle="Pick up where you left off.">
          {categorized.continueExploring.map(item => (
            <FeedCard key={item.id} item={item} onClick={onSelectContent} />
          ))}
        </FeedSection>

        <FeedSection title="Recently Saved">
          {categorized.recentlySaved.map(item => (
            <FeedCard key={item.id} item={item} onClick={onSelectContent} />
          ))}
        </FeedSection>
      </div>
    </div>
  );
}
