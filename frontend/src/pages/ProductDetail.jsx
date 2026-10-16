import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { IconEdit, IconHeart } from '../components/icons';
import AddToCartButton from '../components/AddToCartButton';
import StarRating from '../components/StarRating';
import ReviewsSection from '../components/reviews/ReviewsSection';
import { useCart } from '../store/CartContext';
import { useAuth } from '../store/AuthContext';
import { useFavorites } from '../store/FavoritesContext';

export default function ProductDetail() {
  const { id } = useParams();
  const [p, setP] = useState(null);
  const { add } = useCart();
  const { user, isAdmin } = useAuth();
  const { isFavorite, toggleFavorite } = useFavorites();
  const fav = p ? isFavorite(p.id) : false;
  const navigate = useNavigate();
  const [freeShip, setFreeShip] = useState(false);
  const [activeIdx, setActiveIdx] = useState(0);
  const [lightbox, setLightbox] = useState(false);
  const thumbRef = useRef(null);
  const dragRef = useRef({ down: false, startX: 0, startLeft: 0, moved: false });
  const handleSummary = useCallback((s) => {
    setP((prev) => prev ? { ...prev, averageRating: s.averageRating, reviewCount: s.reviewCount } : prev);
  }, []);

  useEffect(() => {
    setActiveIdx(0);
    setP(null);
    api.getProduct(id).then((prod) => {
      setP(prod);
      setActiveIdx(0);
      api.freeShipping(prod.price).then(setFreeShip).catch(() => {});
    }).catch(console.error);
  }, [id]);

  // Tekerlek ile yatay kaydırma (passive:false ile sayfa kaymasını engelle)
  // Not: Hooks kuralları gereği erken return'den önce olmalı
  useEffect(() => {
    const el = thumbRef.current;
    if (!el) return;
    const handler = (e) => {
      if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) {
        e.preventDefault();
        el.scrollLeft += e.deltaY;
      }
    };
    el.addEventListener('wheel', handler, { passive: false });
    return () => el.removeEventListener('wheel', handler);
  }, [p]);

  // Lightbox açıkken Esc / ok tuşları
  useEffect(() => {
    if (!lightbox) return;
    const onKey = (e) => {
      if (e.key === 'Escape') setLightbox(false);
      if (e.key === 'ArrowLeft') setActiveIdx((i) => (i - 1 + (p?.images?.length || 1)) % (p?.images?.length || 1));
      if (e.key === 'ArrowRight') setActiveIdx((i) => (i + 1) % (p?.images?.length || 1));
    };
    document.addEventListener('keydown', onKey);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = '';
    };
  }, [lightbox, p]);

  const toggleFav = async () => {
    try {
      await toggleFavorite(p);
    } catch (err) {
      alert(err.message);
    }
  };

  const buyNow = async () => {
    await add(p);
    navigate(user ? '/checkout' : '/login');
  };

  if (!p) return <Layout><p>Yükleniyor...</p></Layout>;
  const images = p.images?.length ? p.images : [];
  const imgUrls = images.length
    ? images.map((img) => api.imageUrl(img.url))
    : [api.primaryImage(p)];
  const total = imgUrls.length;
  const safeIdx = total ? activeIdx % total : 0;

  const prev = () => setActiveIdx((i) => (i - 1 + total) % total);
  const next = () => setActiveIdx((i) => (i + 1) % total);

  const scrollActiveIntoView = (idx) => {
    const el = thumbRef.current;
    if (!el) return;
    const child = el.children[idx];
    if (child) child.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
  };

  const goTo = (idx) => {
    setActiveIdx(idx);
    scrollActiveIntoView(idx);
  };

  const onDragStart = (e) => {
    const el = thumbRef.current;
    if (!el) return;
    dragRef.current = { down: true, startX: e.clientX, startLeft: el.scrollLeft, moved: false };
  };
  const onDragMove = (e) => {
    const d = dragRef.current;
    const el = thumbRef.current;
    if (!d.down || !el) return;
    const dx = e.clientX - d.startX;
    if (Math.abs(dx) > 4) d.moved = true;
    el.scrollLeft = d.startLeft - dx;
  };
  const onDragEnd = () => {
    dragRef.current.down = false;
  };

  return (
    <Layout>
      <div className="detail">
        <div className="gallery">
          <div className="gallery-main">
            <img
              src={imgUrls[safeIdx]}
              alt={p.name}
              draggable={false}
              className="zoomable"
              onClick={() => setLightbox(true)}
              title="Büyütmek için tıkla"
            />
            {total > 1 && (
              <>
                <button className="gallery-nav left" onClick={(e) => { e.stopPropagation(); prev(); }} aria-label="Önceki resim">‹</button>
                <button className="gallery-nav right" onClick={(e) => { e.stopPropagation(); next(); }} aria-label="Sonraki resim">›</button>
                <span className="gallery-count">{safeIdx + 1} / {total}</span>
              </>
            )}
          </div>
          {total > 1 && (
            <div
              className="thumb-scroll"
              ref={thumbRef}
              onMouseDown={onDragStart}
              onMouseMove={onDragMove}
              onMouseUp={onDragEnd}
              onMouseLeave={onDragEnd}
            >
              {imgUrls.map((src, i) => (
                <button
                  key={images[i]?.id ?? i}
                  className={`thumb ${i === safeIdx ? 'active' : ''}`}
                  onClick={(e) => {
                    // Sürükleme sonrası click'i yut
                    if (dragRef.current.moved) {
                      dragRef.current.moved = false;
                      e.preventDefault();
                      return;
                    }
                    goTo(i);
                  }}
                  title={`${i + 1}. resim`}
                >
                  <img src={src} alt="" draggable={false} />
                </button>
              ))}
            </div>
          )}
        </div>
        <div>
          <div className="card-cat">{p.categoryName}{p.brandName ? ` - ${p.brandName}` : ''}</div>
          <h2>{p.name}</h2>
          <button className="rev-scroll-link" onClick={() => document.getElementById('reviews')?.scrollIntoView({ behavior: 'smooth' })}>
            <StarRating value={p.averageRating} reviewCount={p.reviewCount ?? 0} size={16} />
          </button>
          <p className="muted">{p.description}</p>
          <div className="card-price big">{Number(p.price).toLocaleString('tr-TR')} ₺</div>
          {freeShip && <span className="ship-badge inline">Kargo Bedava</span>}
          {(p.productCode || p.barcode) && (
            <div className="muted">Ürün Kodu: {p.productCode || '-'} | Barkod: {p.barcode || '-'}</div>
          )}
          <br />
          <div className="row">
            <button className="btn buy-btn grow" onClick={buyNow}>Şimdi Al</button>
            <AddToCartButton product={p} />
            <button className={`icon-btn fav ${fav ? 'on' : ''}`} onClick={toggleFav} title={fav ? 'Favoriden çıkar' : 'Favoriye ekle'}>
              <IconHeart filled={fav} />
            </button>
          </div>
          {isAdmin && <p><Link className="hbtn edit-link" to={`/admin/products/${p.id}`}><IconEdit /> Ürünü Düzenle</Link></p>}
        </div>
      </div>
      <ReviewsSection productId={p.id} onSummaryChange={handleSummary} />
      {lightbox && (
        <div className="lightbox" onClick={() => setLightbox(false)}>
          <button className="lightbox-close" onClick={() => setLightbox(false)} aria-label="Kapat">×</button>
          {total > 1 && (
            <>
              <button
                className="lightbox-nav left"
                onClick={(e) => { e.stopPropagation(); prev(); }}
                aria-label="Önceki resim"
              >‹</button>
              <button
                className="lightbox-nav right"
                onClick={(e) => { e.stopPropagation(); next(); }}
                aria-label="Sonraki resim"
              >›</button>
            </>
          )}
          <img
            className="lightbox-img"
            src={imgUrls[safeIdx]}
            alt={p.name}
            onClick={(e) => e.stopPropagation()}
          />
          {total > 1 && <span className="gallery-count lightbox-count">{safeIdx + 1} / {total}</span>}
        </div>
      )}
    </Layout>
  );
}
