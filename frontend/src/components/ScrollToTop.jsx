import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

// Rota değiştiğinde sayfayı en üste al; özellikle mobilde footer linklerine
// tıklandığında sayfa aşağıda kalmaması için.
export default function ScrollToTop() {
  const { pathname, search, hash } = useLocation();
  useEffect(() => {
    if (hash) {
      const el = document.querySelector(hash);
      if (el) { el.scrollIntoView(); return; }
    }
    window.scrollTo(0, 0);
  }, [pathname, search, hash]);
  return null;
}
