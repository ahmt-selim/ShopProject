/* eslint-disable react/prop-types -- proje genelinde propTypes kullanılmıyor (düz JSX) */
/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useEffect, useState } from 'react';

const DESKTOP_QUERY = '(min-width: 801px)';
const STORAGE_KEY = 'sidebarOpen';

function defaultOpen() {
  if (typeof window === 'undefined') return true;
  return window.matchMedia(DESKTOP_QUERY).matches;
}

const SidebarContext = createContext(null);

export function SidebarProvider({ children }) {
  const [open, setOpen] = useState(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved !== null) return saved === 'true';
    } catch {
      // ignore
    }
    return defaultOpen();
  });

  useEffect(() => {
    const mq = window.matchMedia(DESKTOP_QUERY);
    const handler = (e) => {
      setOpen(e.matches);
      try {
        localStorage.setItem(STORAGE_KEY, String(e.matches));
      } catch {
        // ignore
      }
    };
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, []);

  const toggle = () => {
    setOpen((prev) => {
      const next = !prev;
      try {
        localStorage.setItem(STORAGE_KEY, String(next));
      } catch {
        // ignore
      }
      return next;
    });
  };

  return (
    <SidebarContext.Provider value={{ open, toggle }}>
      {children}
    </SidebarContext.Provider>
  );
}

export function useSidebar() {
  const ctx = useContext(SidebarContext);
  if (!ctx) throw new Error('useSidebar must be used within SidebarProvider');
  return ctx;
}