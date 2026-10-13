import { Link } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';
import { useCart } from '../store/CartContext';
import { useFavorites } from '../store/FavoritesContext';

export default function Favorites() {
  const { user } = useAuth();
  const { add } = useCart();
  const { favorites, toggleFavorite } = useFavorites();

  return (
    <Layout>
      <h2>Favorilerim</h2>
      {!user && <p className="muted">Giriş yapmadın — favorilerin bu cihazda saklanıyor, giriş yapınca hesabına aktarılır.</p>}
      {favorites.length === 0 ? (
        <p>Henüz favori yok. Ürün kartındaki kalp ile ekle.</p>
      ) : (
        <div className="grid">
          {favorites.map((p) => (
            <div key={p.id} className="card">
              <Link to={`/product/${p.id}`}><img src={api.imageUrl(p.imageUrl)} alt={p.name} /></Link>
              <div className="card-body">
                <Link to={`/product/${p.id}`} className="card-title">{p.name}</Link>
                <div className="card-price">{Number(p.price).toLocaleString('tr-TR')} ₺</div>
                <div className="row">
                  <button className="btn grow" onClick={() => add(p)}>Sepete Ekle</button>
                  <button className="hbtn" onClick={() => toggleFavorite(p).catch((e) => alert(e.message))}>Kaldır</button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </Layout>
  );
}
