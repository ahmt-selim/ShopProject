import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useFavorites } from '../store/FavoritesContext';
import { IconHeart } from './icons';
import AddToCartButton from './AddToCartButton';
import StarRating from './StarRating';

export default function ProductCard({ product }) {
  const { isFavorite, toggleFavorite } = useFavorites();
  const fav = isFavorite(product.id);
  const [freeShip, setFreeShip] = useState(false);
  const [hoverIndex, setHoverIndex] = useState(0);
  const [hovering, setHovering] = useState(false);

  const images = (product.images || [])
    .map((i) => api.imageUrl(i.url))
    .filter(Boolean)
    .slice(0, 4);
  if (images.length === 0) images.push(api.primaryImage(product));

  useEffect(() => {
    api.freeShipping(product.price).then(setFreeShip).catch(() => {});
  }, [product.price]);

  const onHeart = async () => {
    try {
      await toggleFavorite(product);
    } catch (err) {
      alert(err.message);
    }
  };

  return (
    <div className="card">
      <Link
        to={`/urunler/${product.slug || product.id}`}
        className="card-img"
        onMouseEnter={() => setHovering(true)}
        onMouseLeave={() => { setHovering(false); setHoverIndex(0); }}
        onMouseMove={(e) => {
          if (images.length <= 1) return;
          const rect = e.currentTarget.getBoundingClientRect();
          const x = e.clientX - rect.left;
          const idx = Math.min(images.length - 1, Math.max(0, Math.floor((x / rect.width) * images.length)));
          setHoverIndex(idx);
        }}
      >
        <img src={images[hovering ? hoverIndex : 0]} alt={product.name} loading="lazy" />
        {hovering && images.length > 1 && (
          <div style={{ position: 'absolute', bottom: 6, left: 0, right: 0, display: 'flex', justifyContent: 'center', gap: 4, pointerEvents: 'none' }}>
            {images.map((_, i) => (
              <span key={i} style={{ width: 6, height: 6, borderRadius: '50%', background: i === hoverIndex ? '#fff' : 'rgba(255,255,255,0.5)' }} />
            ))}
          </div>
        )}
        {freeShip && <span className="ship-badge">Kargo Bedava</span>}
      </Link>
      <div className="card-body">
        <div className="card-cat">{product.categoryName}</div>
        <Link to={`/urunler/${product.slug || product.id}`} className="card-title" title={product.name}>{product.name}</Link>
        <StarRating value={product.averageRating} reviewCount={product.reviewCount ?? 0} size={14} />
        <div className="card-price">
          {product.listPrice && Number(product.listPrice) > Number(product.price) ? (
            <span style={{ textDecoration: 'line-through', color: '#999', marginRight: 6, fontSize: '0.9em' }}>
              {Number(product.listPrice).toLocaleString('tr-TR')} ₺
            </span>
          ) : null}
          {Number(product.price).toLocaleString('tr-TR')} ₺
        </div>
        <div className="row">
          <AddToCartButton product={product} />
          <button className={`icon-btn fav ${fav ? 'on' : ''}`} onClick={onHeart} title={fav ? 'Favoriden çıkar' : 'Favoriye ekle'}>
            <IconHeart filled={fav} />
          </button>
        </div>
      </div>
    </div>
  );
}
