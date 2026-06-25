import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { request } from '../../api';

export interface MemoryTimelineEvent {
  id: string;
  monthYear: string;
  theme: string;
  summary: string;
  itemCount: number;
}

export interface MemoryRecommendation {
  id: string;
  type: string;
  title: string;
  reason: string;
  content?: { id: string; title: string; thumbnailUrl?: string };
  collection?: { id: string; title: string };
}

export function useMemoryTimeline() {
  return useQuery({
    queryKey: ['memory', 'timeline'],
    queryFn: async () => {
      const response = await request('/memory/timeline');
      return response; // Return full IntelligenceResponse
    }
  });
}

export function useMemoryRecommendations() {
  return useQuery({
    queryKey: ['memory', 'recommendations'],
    queryFn: async () => {
      const response = await request('/memory/recommendations');
      return response; // Return full IntelligenceResponse
    }
  });
}

export function useGenerateTimeline() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const response = await request('/memory/timeline/generate', {
        method: 'POST'
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['memory', 'timeline'] });
    }
  });
}
