import { useEffect } from 'react';
import { api } from '../../api/client';

// Küçük resme tıklayınca büyük gösterim; Esc / ok tuşları, mevcut .lightbox CSS'i kullanır.
export default function ReviewGallery({ images, index, onClose, onNav }) {
  useEffect(() => {
    const onKey = (e) => {
      if (e.key === 'Escape') onClose();
      if (e.key === 'ArrowLeft') onNav(-1);
      if (e.key === 'ArrowRight') onNav(1);
    };
    document.addEventListener('keydown', onKey);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = '';
    };
  }, [onClose, onNav]);

  const total = images.length;
  const safe = total ? ((index % total) + total) % total : 0;

  return (
    <div className="lightbox" onClick={onClose} role="dialog" aria-modal="true" aria-label="Yorum resmi büyütme">
      <button className="lightbox-close" onClick={onClose} aria-label="Kapat">×</button>
      {total > 1 && (
        <>
          <button className="lightbox-nav left" aria-label="Önceki resim"
            onClick={(e) => { e.stopPropagation(); onNav(-1); }}>‹</button>
          <button className="lightbox-nav right" aria-label="Sonraki resim"
            onClick={(e) => { e.stopPropagation(); onNav(1); }}>›</button>
        </>
      )}
      <img className="lightbox-img" src={api.imageUrl(images[safe]?.url)} alt=""
        onClick={(e) => e.stopPropagation()} />
      {total > 1 && <span className="gallery-count lightbox-count">{safe + 1} / {total}</span>}
    </div>
  );
}
