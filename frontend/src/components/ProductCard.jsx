import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useFavorites } from '../store/FavoritesContext';
import { IconHeart } from './icons';
import AddToCartButton from './AddToCartButton';

export default function ProductCard({ product }) {
  const { isFavorite, toggleFavorite } = useFavorites();
  const fav = isFavorite(product.id);
  const [freeShip, setFreeShip] = useState(false);

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
      <Link to={`/product/${product.id}`} className="card-img">
        <img src={api.primaryImage(product)} alt={product.name} loading="lazy" />
        {freeShip && <span className="ship-badge">Kargo Bedava</span>}
      </Link>
      <div className="card-body">
        <div className="card-cat">{product.categoryName}</div>
        <Link to={`/product/${product.id}`} className="card-title">{product.name}</Link>
        <div className="card-price">{Number(product.price).toLocaleString('tr-TR')} ₺</div>
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
