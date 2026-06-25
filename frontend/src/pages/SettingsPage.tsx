import { useLayoutStore } from '../stores/layoutStore';
import { Sun, Moon } from 'lucide-react';

export default function SettingsPage() {
  const { theme, toggleTheme } = useLayoutStore();

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <h1 className="text-3xl font-bold tracking-tight mb-8">Settings</h1>
      
      <section className="space-y-4">
        <h2 className="text-xl font-semibold border-b border-border pb-2">Appearance</h2>
        <div className="flex items-center justify-between bg-card p-4 rounded-xl border border-border/50 shadow-sm">
          <div>
            <h3 className="font-medium text-foreground">Theme</h3>
            <p className="text-sm text-muted-foreground">Switch between light and dark mode</p>
          </div>
          <button
            onClick={toggleTheme}
            className="flex items-center gap-2 px-4 py-2 bg-secondary text-secondary-foreground rounded-lg font-medium hover:bg-secondary/80 transition-colors"
          >
            {theme === 'dark' ? <Sun size={18} /> : <Moon size={18} />}
            {theme === 'dark' ? 'Light Mode' : 'Dark Mode'}
          </button>
        </div>
      </section>
    </div>
  );
}
