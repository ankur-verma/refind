import { create } from 'zustand';

type Theme = 'dark' | 'light';

interface LayoutState {
  isAssistantOpen: boolean;
  toggleAssistant: () => void;
  setAssistantOpen: (open: boolean) => void;
  theme: Theme;
  toggleTheme: () => void;
  setTheme: (theme: Theme) => void;
}

// Helper to initialize theme from localStorage or system preference
const getInitialTheme = (): Theme => {
  const saved = localStorage.getItem('refind-theme');
  if (saved === 'light' || saved === 'dark') return saved;
  // We default to dark as per design system, but check system pref just in case
  if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) {
    // Actually, design system defaults to dark mode heavily.
    return 'dark'; 
  }
  return 'dark';
};

const applyTheme = (theme: Theme) => {
  if (theme === 'light') {
    document.documentElement.classList.add('light');
    document.documentElement.classList.remove('dark');
  } else {
    document.documentElement.classList.add('dark');
    document.documentElement.classList.remove('light');
  }
  localStorage.setItem('refind-theme', theme);
};

// Initial setup
const initialTheme = getInitialTheme();
applyTheme(initialTheme);

export const useLayoutStore = create<LayoutState>((set) => ({
  isAssistantOpen: false,
  toggleAssistant: () => set((state) => ({ isAssistantOpen: !state.isAssistantOpen })),
  setAssistantOpen: (open) => set({ isAssistantOpen: open }),
  theme: initialTheme,
  toggleTheme: () => set((state) => {
    const newTheme = state.theme === 'dark' ? 'light' : 'dark';
    applyTheme(newTheme);
    return { theme: newTheme };
  }),
  setTheme: (theme) => set(() => {
    applyTheme(theme);
    return { theme };
  }),
}));
