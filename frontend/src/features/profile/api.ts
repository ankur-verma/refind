import { useQuery } from '@tanstack/react-query';
import { API_BASE_URL, request } from '../../api';

export const profileKeys = {
  all: ['profile'] as const,
  overview: () => [...profileKeys.all, 'overview'] as const,
  interests: () => [...profileKeys.all, 'interests'] as const,
  categories: () => [...profileKeys.all, 'categories'] as const,
  upcomingPlans: () => [...profileKeys.all, 'upcoming-plans'] as const,
  revisited: () => [...profileKeys.all, 'revisited'] as const,
  activityGraph: () => [...profileKeys.all, 'activity-graph'] as const,
};

export function useProfileOverview() {
  return useQuery({
    queryKey: profileKeys.overview(),
    queryFn: async () => {
      const response = await request(`${API_BASE_URL}/profile/overview`, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      if (response.success === false) throw new Error('Failed to fetch overview');
      return response.data;
    }
  });
}

export function useProfileInterests() {
  return useQuery({
    queryKey: profileKeys.interests(),
    queryFn: async () => {
      const response = await request(`${API_BASE_URL}/profile/interests`, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      if (response.success === false) throw new Error('Failed to fetch interests');
      return response; // Return full IntelligenceResponse to access hasData
    }
  });
}

export function useProfileCategories() {
  return useQuery({
    queryKey: profileKeys.categories(),
    queryFn: async () => {
      const response = await request(`${API_BASE_URL}/profile/categories`, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      if (response.success === false) throw new Error('Failed to fetch categories');
      return response;
    }
  });
}

export function useProfileUpcomingPlans() {
  return useQuery({
    queryKey: profileKeys.upcomingPlans(),
    queryFn: async () => {
      const response = await request(`${API_BASE_URL}/profile/upcoming-plans`, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      if (response.success === false) throw new Error('Failed to fetch upcoming plans');
      return response;
    }
  });
}

export function useProfileRevisited() {
  return useQuery({
    queryKey: profileKeys.revisited(),
    queryFn: async () => {
      const response = await request(`${API_BASE_URL}/profile/revisited`, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      if (response.success === false) throw new Error('Failed to fetch revisited content');
      return response;
    }
  });
}

export function useProfileActivityGraph() {
  return useQuery({
    queryKey: profileKeys.activityGraph(),
    queryFn: async () => {
      const response = await request(`${API_BASE_URL}/profile/activity-graph`, {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      if (response.success === false) throw new Error('Failed to fetch activity graph');
      return response;
    }
  });
}
