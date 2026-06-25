import React from 'react';
import FeedCard from '../../content-feed/components/FeedCard';

interface Props {
  items: any[];
  onSelect: (id: string) => void;
}

export default function CollectionGrid({ items, onSelect }: Props) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6 p-6 md:p-10">
      {items.map(item => (
        <FeedCard key={item.id} item={item} onClick={onSelect} />
      ))}
    </div>
  );
}
