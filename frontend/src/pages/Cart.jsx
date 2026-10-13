import { useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { api } from '../api/client';
import { useCart } from '../store/CartContext';
import { useAuth } from '../store/AuthContext';

export default function Cart() {
  const { items, setQty, remove, clear, totalPrice } = useCart();
  const { user } = useAuth();
  const navigate = useNavigate();

  return (
    <Layout>
      <h2>Sepet</h2>
      {items.length === 0 ? (
        <p>Sepetin boş. Anasayfadan ürün ekle.</p>
      ) : (
        <>
          <div className="cart-list">
            {items.map((i) => (
              <div key={i.id} className="cart-item">
                <img src={api.imageUrl(i.imageUrl)} alt={i.name} />
                <div className="cart-info">
                  <strong>{i.name}</strong>
                  <div className="muted">{Number(i.price).toLocaleString('tr-TR')} ₺</div>
                  <div className="qty">
                    <button onClick={() => setQty(i.id, i.qty - 1)}>-</button>
                    <span>{i.qty}</span>
                    <button onClick={() => setQty(i.id, i.qty + 1)}>+</button>
                  </div>
                </div>
                <button className="link" onClick={() => remove(i.id)}>Kaldır</button>
              </div>
            ))}
          </div>
          <h3>Toplam: {totalPrice.toLocaleString('tr-TR')} ₺</h3>
          <div className="row">
            <button className="btn" onClick={() => navigate(user ? '/checkout' : '/login')}>Siparişi Tamamla</button>
            <button className="hbtn" onClick={clear}>Sepeti Temizle</button>
          </div>
        </>
      )}
    </Layout>
  );
}
