import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { IconHeart } from '../components/icons';
import { useCart } from '../store/CartContext';
import { useAuth } from '../store/AuthContext';
import { useFavorites } from '../store/FavoritesContext';

export default function ProductDetail() {
  const { id } = useParams();
  const [p, setP] = useState(null);
  const { add } = useCart();
  const { isAdmin } = useAuth();
  const { isFavorite, toggleFavorite } = useFavorites();
  const fav = p ? isFavorite(p.id) : false;

  useEffect(() => {
    api.getProduct(id).then(setP).catch(console.error);
  }, [id]);

  const toggleFav = async () => {
    try {
      await toggleFavorite(p);
    } catch (err) {
      alert(err.message);
    }
  };

  if (!p) return <Layout><p>Yükleniyor...</p></Layout>;
  const images = p.images?.length ? p.images : [];

  return (
    <Layout>
      <div className="detail">
        <div>
          <img src={api.primaryImage(p)} alt={p.name} />
          {images.length > 1 && (
            <div className="thumb-row">
              {images.map((img) => <img key={img.id} src={api.imageUrl(img.url)} alt="" />)}
            </div>
          )}
        </div>
        <div>
          <div className="card-cat">{p.categoryName}{p.brandName ? ` - ${p.brandName}` : ''}</div>
          <h2>{p.name}</h2>
          <p className="muted">{p.description}</p>
          <div className="card-price big">{Number(p.price).toLocaleString('tr-TR')} ₺</div>
          <div className="muted">Stok: {p.stock}</div>
          {(p.productCode || p.barcode) && (
            <div className="muted">Ürün Kodu: {p.productCode || '-'} | Barkod: {p.barcode || '-'}</div>
          )}
          <div className="muted">KDV: %{p.vatRate}</div>
          <br />
          <div className="row">
            <button className="btn grow" onClick={() => add(p)}>Sepete Ekle</button>
            <button className={`icon-btn fav ${fav ? 'on' : ''}`} onClick={toggleFav} title={fav ? 'Favoriden çıkar' : 'Favoriye ekle'}>
              <IconHeart filled={fav} />
            </button>
          </div>
          {isAdmin && <p><Link to="/admin">Admin panelinde düzenle →</Link></p>}
        </div>
      </div>
    </Layout>
  );
}
