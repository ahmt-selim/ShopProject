import { createContext, useContext, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import { useAuth } from './AuthContext';

const CartContext = createContext(null);

// Giriş yoksa sepet localStorage'da, giriş varsa sunucuda (kullanıcı bazlı).
// Giriş yapılınca misafir sepeti hesaba aktarılır.
export function CartProvider({ children }) {
  const { user } = useAuth();
  const [serverItems, setServerItems] = useState([]);
  const [guest, setGuest] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem('cart') || '[]');
    } catch {
      return [];
    }
  });
  const mergedFor = useRef(null);

  useEffect(() => {
    localStorage.setItem('cart', JSON.stringify(guest));
  }, [guest]);

  const loadServer = () => api.getCart().then((d) => setServerItems(d.items)).catch(() => {});

  useEffect(() => {
    if (user) {
      if (guest.length > 0 && mergedFor.current !== user.id) {
        mergedFor.current = user.id;
        (async () => {
          for (const g of guest) {
            try {
              await api.addCartItem(g.id, g.qty);
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

  const items = user ? serverItems : guest;

  const add = async (product, qty = 1) => {
    if (user) {
      try {
        const cart = await api.addCartItem(product.id, qty);
        setServerItems(cart.items);
      } catch (err) {
        alert(err.message);
      }
      return;
    }
    setGuest((prev) => {
      const found = prev.find((i) => i.id === product.id);
      if (found) {
        return prev.map((i) =>
          i.id === product.id ? { ...i, qty: i.qty + qty } : i
        );
      }
      return [...prev, { id: product.id, name: product.name, price: product.price, imageUrl: product.imageUrl || api.primaryImage(product), qty }];
    });
  };

  const remove = async (id) => {
    if (user) {
      try {
        const cart = await api.removeCartItem(id);
        setServerItems(cart.items);
      } catch (err) {
        alert(err.message);
      }
      return;
    }
    setGuest((prev) => prev.filter((i) => i.id !== id));
  };

  const setQty = async (id, qty) => {
    if (user) {
      try {
        const cart = await api.setCartItem(id, qty);
        setServerItems(cart.items);
      } catch (err) {
        alert(err.message);
      }
      return;
    }
    setGuest((prev) =>
      qty <= 0
        ? prev.filter((i) => i.id !== id)
        : prev.map((i) => (i.id === id ? { ...i, qty } : i))
    );
  };

  const clear = async () => {
    if (user) {
      try {
        await api.clearCart();
        setServerItems([]);
      } catch (err) {
        alert(err.message);
      }
      return;
    }
    setGuest([]);
  };

  const totalCount = items.reduce((s, i) => s + i.qty, 0);
  const totalPrice = items.reduce((s, i) => s + i.qty * i.price, 0);

  return (
    <CartContext.Provider value={{ items, add, remove, setQty, clear, totalCount, totalPrice }}>
      {children}
    </CartContext.Provider>
  );
}

export const useCart = () => useContext(CartContext);
