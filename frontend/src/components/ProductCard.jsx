import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useCart } from '../store/CartContext';
import { useFavorites } from '../store/FavoritesContext';
import { IconHeart } from './icons';

export default function ProductCard({ product }) {
  const { add } = useCart();
  const { isFavorite, toggleFavorite } = useFavorites();
  const fav = isFavorite(product.id);

  const onHeart = async () => {
    try {
      await toggleFavorite(product);
    } catch (err) {
      alert(err.message);
    }
  };

  return (
    <div className="card">
      <Link to={`/product/${product.id}`}>
        <img src={api.primaryImage(product)} alt={product.name} loading="lazy" />
      </Link>
      <div className="card-body">
        <div className="card-cat">{product.categoryName}</div>
        <Link to={`/product/${product.id}`} className="card-title">{product.name}</Link>
        <div className="card-price">{Number(product.price).toLocaleString('tr-TR')} ₺</div>
        <div className="row">
          <button className="btn grow" onClick={() => add(product)}>Sepete Ekle</button>
          <button className={`icon-btn fav ${fav ? 'on' : ''}`} onClick={onHeart} title={fav ? 'Favoriden çıkar' : 'Favoriye ekle'}>
            <IconHeart filled={fav} />
          </button>
        </div>
      </div>
    </div>
  );
}
