import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';
import { useCart } from '../store/CartContext';

export default function Checkout() {
  const { user } = useAuth();
  const { items, clear } = useCart();
  const [addresses, setAddresses] = useState([]);
  const [selected, setSelected] = useState(null);
  const [error, setError] = useState('');
  const [placing, setPlacing] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    api.getAddresses().then((a) => {
      setAddresses(a);
      if (a.length > 0) setSelected(a[0].id);
    }).catch(console.error);
  }, []);

  if (!user) return <Layout><p>Sipariş için <Link to="/login">giriş yapın</Link>.</p></Layout>;
  if (items.length === 0) return <Layout><p>Sepetin boş.</p></Layout>;

  const subtotal = items.reduce((s, i) => s + i.qty * i.price, 0);

  const confirm = async () => {
    setError('');
    if (!selected) {
      setError('Teslimat adresi seçin.');
      return;
    }
    setPlacing(true);
    try {
      const order = await api.checkout(selected, items.map((i) => ({ productId: i.id, qty: i.qty })));
      clear();
      navigate(`/account/orders/${order.id}`);
    } catch (err) {
      setError(err.message);
    } finally {
      setPlacing(false);
    }
  };

  return (
    <Layout>
      <h2>Siparişi Tamamla</h2>
      {error && <div className="error">{error}</div>}
      <div className="account-grid">
        <div>
          <h3>Teslimat Adresi</h3>
          {addresses.length === 0 && <p>Adresin yok. <Link to="/account/addresses">Adreslerim</Link> sayfasından ekle.</p>}
          {addresses.map((a) => (
            <label key={a.id} className={`addr-card radio ${selected === a.id ? 'sel' : ''}`}>
              <input type="radio" checked={selected === a.id} onChange={() => setSelected(a.id)} />
              <span><strong>{a.title || 'Adres'} - {a.city}/{a.district}</strong><br />{a.fullName} - {a.phone}<br /><span className="muted">{a.detail}</span></span>
            </label>
          ))}
        </div>
        <div>
          <h3>Sipariş Özeti</h3>
          <div className="cart-list">
            {items.map((i) => (
              <div key={i.id} className="cart-item">
                <img src={api.imageUrl(i.imageUrl)} alt={i.name} />
                <div className="cart-info">
                  <strong>{i.name}</strong>
                  <div className="muted">{i.qty} x {Number(i.price).toLocaleString('tr-TR')} ₺</div>
                </div>
                <strong>{(i.qty * i.price).toLocaleString('tr-TR')} ₺</strong>
              </div>
            ))}
          </div>
          <h3>Ara toplam: {subtotal.toLocaleString('tr-TR')} ₺</h3>
          <p className="muted">Kargo sipariş oluşturulurken hesaplanır.</p>
          <button className="btn" onClick={confirm} disabled={placing}>
            {placing ? 'Oluşturuluyor...' : 'Siparişi Onayla'}
          </button>
        </div>
      </div>
    </Layout>
  );
}
