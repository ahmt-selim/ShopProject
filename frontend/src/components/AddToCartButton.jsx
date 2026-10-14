import { useEffect, useRef, useState } from 'react';
import { useCart } from '../store/CartContext';

// idle -> loading (spinner) -> added (yeşil + kayan yazı, 1sn) -> idle
export default function AddToCartButton({ product, qty = 1, className = 'btn grow' }) {
  const { add } = useCart();
  const [state, setState] = useState('idle');
  const [back, setBack] = useState(false);
  const timer = useRef(null);

  useEffect(() => () => clearTimeout(timer.current), []);

  const click = async () => {
    if (state !== 'idle') return;
    setBack(false);
    setState('loading');
    await add(product, qty);
    setState('added');
    timer.current = setTimeout(() => {
      setBack(true);
      setState('idle');
    }, 1000);
  };

  return (
    <button className={`${className} add-btn ${state}`} onClick={click} disabled={state === 'loading'}>
      {state === 'loading' && <span className="spinner" />}
      {state === 'added' && <span key="added" className="added-msg">Sepete eklendi</span>}
      {state === 'idle' && <span key="idle" className={back ? 'idle-msg back' : 'idle-msg'}>Sepete Ekle</span>}
    </button>
  );
}
