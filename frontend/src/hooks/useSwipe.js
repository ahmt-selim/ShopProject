import { useRef } from 'react';

// Yatay kaydırma (swipe) algılayıcı.
// - Pointer Events kullanır: dokunmatik, kalem ve fare aynı şekilde çalışır.
// - Sürüklerken hedef elemanı parmakla birlikte kaydırır (ref üzerinden, render yok).
// - Dikey hareketler sayfayı kaydırmaya bırakılır (CSS: touch-action: pan-y).
export default function useSwipe({ targetRef, onSwipeLeft, onSwipeRight, threshold = 45, enabled = true }) {
  const state = useRef({ id: null, startX: 0, startY: 0, active: false, horizontal: false, swipedAt: 0 });

  const setTransform = (x, animate) => {
    const el = targetRef?.current;
    if (!el) return;
    el.style.transition = animate ? 'transform .18s ease' : 'none';
    el.style.transform = x ? `translate3d(${x}px, 0, 0)` : '';
  };

  const onPointerDown = (e) => {
    if (!enabled) return;
    if (e.pointerType === 'mouse' && e.button !== 0) return;
    state.current = {
      id: e.pointerId,
      startX: e.clientX,
      startY: e.clientY,
      active: true,
      horizontal: false,
      swipedAt: state.current.swipedAt,
    };
  };

  const onPointerMove = (e) => {
    const s = state.current;
    if (!s.active || s.id !== e.pointerId) return;
    const dx = e.clientX - s.startX;
    const dy = e.clientY - s.startY;
    if (!s.horizontal) {
      // Yatay mı dikey mi olduğuna karar verene kadar bekle
      if (Math.abs(dx) < 8 || Math.abs(dx) <= Math.abs(dy)) return;
      s.horizontal = true;
    }
    setTransform(dx, false);
  };

  const finish = (e) => {
    const s = state.current;
    if (!s.active || s.id !== e.pointerId) return;
    const dx = e.clientX - s.startX;
    s.active = false;
    s.id = null;
    if (s.horizontal) {
      if (Math.abs(dx) >= threshold) {
        s.swipedAt = Date.now();
        // Yeni görsel, kaydırma yönünün karşı tarafından içeri girsin
        const el = targetRef?.current;
        if (el) {
          const w = el.getBoundingClientRect().width || 320;
          el.style.transition = 'none';
          el.style.transform = `translate3d(${dx < 0 ? w : -w}px, 0, 0)`;
          requestAnimationFrame(() => {
            el.style.transition = 'transform .2s ease';
            el.style.transform = '';
          });
        }
        if (dx < 0) onSwipeLeft?.();
        else onSwipeRight?.();
      } else {
        setTransform(0, true);
      }
    }
    s.horizontal = false;
  };

  const cancel = () => {
    state.current.active = false;
    state.current.horizontal = false;
    state.current.id = null;
    setTransform(0, true);
  };

  // Sürükleme sonrası gelen click'i yut (tıklama ile büyütme vs.)
  const wasSwiped = () => Date.now() - state.current.swipedAt < 400;

  return {
    wasSwiped,
    handlers: {
      onPointerDown,
      onPointerMove,
      onPointerUp: finish,
      onPointerCancel: cancel,
      onPointerLeave: (e) => {
        if (e.pointerType === 'mouse') finish(e);
      },
    },
  };
}
