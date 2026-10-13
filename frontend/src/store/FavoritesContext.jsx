import { createContext, useContext, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import { useAuth } from './AuthContext';

const FavoritesContext = createContext(null);

// Giriş yoksa favoriler localStorage'da tutulur, giriş yapılınca hesaba aktarılır.
export function FavoritesProvider({ children }) {
  const { user } = useAuth();
  const [serverItems, setServerItems] = useState([]);
  const [guest, setGuest] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem('guest_favs') || '[]');
    } catch {
      return [];
    }
  });
  const mergedFor = useRef(null);

  useEffect(() => {
    localStorage.setItem('guest_favs', JSON.stringify(guest));
  }, [guest ]);

  const loadServer = () => api.getFavorites().then(setServerItems).catch(() => {});

  useEffect(() => {
    if (user) {
      if (guest.length > 0 && mergedFor.current !== user.id) {
        mergedFor.current = user.id;
        (async () => {
          for (const g of guest) {
            try {
              await api.addFavorite(g.id);
            } catch {
              /* silinmiş ürün vb. atlanır */
            }
          }
          setGuest([]);
          loadServer();
        })();
      } else {
        loadServer();
      }
    }
  }, [user]); // eslint-disable-line react-hooks/exhaustive-deps

  const favorites = user ? serverItems : guest;
  const isFavorite = (id) => favorites.some((i) => i.id === Number(id));

  const toggleFavorite = async (product) => {
    const pid = product.id;
    if (user) {
      if (serverItems.some((i) => i.id === pid)) await api.removeFavorite(pid);
      else await api.addFavorite(pid);
      loadServer();
    } else {
      setGuest((prev) =>
        prev.some((i) => i.id === pid)
          ? prev.filter((i) => i.id !== pid)
          : [...prev, { id: pid, name: product.name, price: product.price, imageUrl: product.imageUrl || api.primaryImage(product) }]
      );
    }
  };

  return (
    <FavoritesContext.Provider value={{ favorites, isFavorite, toggleFavorite }}>
      {children}
    </FavoritesContext.Provider>
  );
}

export const useFavorites = () => useContext(FavoritesContext);
