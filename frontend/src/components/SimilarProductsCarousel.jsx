import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import PropTypes from 'prop-types';
import { api } from '../api/client';

export default function SimilarProductsCarousel({ productId }) {
  const [items, setItems] = useState(null); // null = loading
  const [error, setError] = useState(false);
  const scrollRef = useRef(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  useEffect(() => {
    // Race-condition koruması: eski isteği iptal et.
    let cancelled = false;
    setItems(null);
    setError(false);
    api.getSimilarProducts(productId)
      .then((data) => {
        if (cancelled) return;
        setItems(Array.isArray(data) ? data : []);
      })
      .catch(() => {
        if (cancelled) return;
        setError(true);
        setItems([]);
      });
    return () => { cancelled = true; };
  }, [productId]);

  const updateScrollState = () => {
    const el = scrollRef.current;
    if (!el) return;
    setCanScrollLeft(el.scrollLeft > 4);
    setCanScrollRight(el.scrollLeft + el.clientWidth < el.scrollWidth - 4);
  };

  useEffect(() => {
    const el = scrollRef.current;
    if (!el) return;
    updateScrollState();
    el.addEventListener('scroll', updateScrollState);
    const ro = new ResizeObserver(updateScrollState);
    ro.observe(el);
    return () => {
      el.removeEventListener('scroll', updateScrollState);
      ro.disconnect();
    };
  }, [items]);

  if (error) return null;
  if (items === null) {
    return (
      <div className="similar-section">
        <h3>Benzer Ürünler</h3>
        <div className="page-spinner"><span className="spinner" /></div>
      </div>
    );
  }
  if (!items || items.length === 0) return null;

  const scrollBy = (dir) => {
    const el = scrollRef.current;
    if (!el) return;
    el.scrollBy({ left: dir * 320, behavior: 'smooth' });
  };

  return (
    <section className="similar-section">
      <h3>Benzer Ürünler</h3>
      <div className="similar-carousel-wrap">
        {canScrollLeft && (
          <button className="similar-nav left" onClick={() => scrollBy(-1)} aria-label="Sola kaydır">‹</button>
        )}
        <div className="similar-carousel" ref={scrollRef}>
          {items.map((p) => (
            <Link
              key={p.id}
              to={`/product/${p.id}`}
              className="similar-card"
              onClick={() => window.scrollTo(0, 0)}
            >
              <div className="similar-card-img">
                <img src={api.imageUrl(p.imageUrl) || api.primaryImage(p)} alt={p.name} />
                {Number(p.stock) <= 0 && <span className="similar-badge oos">Tükendi</span>}
              </div>
              <div className="similar-card-body">
                <div className="similar-card-name">{p.name}</div>
                <div className="card-price">
                  {p.listPrice && Number(p.listPrice) > Number(p.price) ? (
                    <span style={{ textDecoration: 'line-through', color: '#999', marginRight: 6, fontSize: '0.85em' }}>
                      {Number(p.listPrice).toLocaleString('tr-TR')} ₺
                    </span>
                  ) : null}
                  {Number(p.price).toLocaleString('tr-TR')} ₺
                </div>
              </div>
            </Link>
          ))}
        </div>
        {canScrollRight && (
          <button className="similar-nav right" onClick={() => scrollBy(1)} aria-label="Sağa kaydır">›</button>
        )}
      </div>
    </section>
  );
}

SimilarProductsCarousel.propTypes = {
  productId: PropTypes.oneOfType([PropTypes.number, PropTypes.string]).isRequired,
};