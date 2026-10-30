import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';
import { useCart } from '../store/CartContext';

export default function Checkout() {
  const { user } = useAuth();
  const { items, totalPrice } = useCart();
  const [addresses, setAddresses] = useState([]);
  const [selected, setSelected] = useState(null);
  const [error, setError] = useState('');
  const [placing, setPlacing] = useState(false);
  const [formHtml, setFormHtml] = useState('');
  const formRef = useRef(null);

  useEffect(() => {
    api.getAddresses().then((a) => {
      setAddresses(a);
      if (a.length > 0) setSelected(a[0].id);
    }).catch(console.error);
  }, []);

  useEffect(() => {
    if (!formHtml || !formRef.current) return;
    const container = formRef.current;
    container.innerHTML = formHtml;
    // script'leri elle çalıştır
    const scripts = Array.from(container.querySelectorAll('script'));
    scripts.forEach((oldScript) => {
      const newScript = document.createElement('script');
      if (oldScript.src) newScript.src = oldScript.src;
      else newScript.textContent = oldScript.textContent;
      Array.from(oldScript.attributes).forEach((attr) => {
        if (attr.name !== 'src') newScript.setAttribute(attr.name, attr.value);
      });
      oldScript.parentNode.replaceChild(newScript, oldScript);
    });
  }, [formHtml]);

  if (!user) return <Layout><p>Sipariş için <Link to="/login">giriş yapın</Link>.</p></Layout>;
  if (items.length === 0) return <Layout><p>Sepetin boş.</p></Layout>;

  const subtotal = totalPrice;
  const freeShipping = 1500;
  const shipping = subtotal >= freeShipping ? 0 : 49.99;
  const total = subtotal + shipping;

  const confirm = async () => {
    setError('');
    if (!selected) {
      setError('Teslimat adresi seçin.');
      return;
    }
    setPlacing(true);
    try {
      const res = await api.initializePayment(selected);
      setFormHtml(res.checkoutFormContent || '');
    } catch (err) {
      setError(err.message);
    } finally {
      setPlacing(false);
    }
  };

  const selectedAddr = addresses.find((a) => a.id === selected);

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
              <input type="radio" checked={selected === a.id} onChange={() => setSelected(a.id)} disabled={!!formHtml} />
              <span><strong>{a.title || 'Adres'} - {a.city}/{a.district}</strong><br />{a.fullName} - {a.phone}<br /><span className="muted">{a.detail}</span></span>
            </label>
          ))}
          {selectedAddr && (
            <div className="addr-card sel" style={{ marginTop: 12 }}>
              <strong>Seçilen adres özeti</strong><br />
              {selectedAddr.fullName} - {selectedAddr.phone}<br />
              {selectedAddr.detail}, {selectedAddr.district}/{selectedAddr.city}
            </div>
          )}
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
          <div style={{ marginTop: 12 }}>
            <div>Ara toplam: <strong>{subtotal.toLocaleString('tr-TR')} ₺</strong></div>
            <div>Kargo: <strong>{shipping.toLocaleString('tr-TR')} ₺</strong></div>
            <div>Genel toplam: <strong>{total.toLocaleString('tr-TR')} ₺</strong></div>
          </div>
          <p className="muted" style={{ marginTop: 8 }}>
            Ödeme iyzico güvencesiyle alınır, kart bilgileriniz bizimle paylaşılmaz.
          </p>
          {!formHtml && (
            <button className="btn" onClick={confirm} disabled={placing}>
              {placing ? 'Oluşturuluyor...' : 'Ödemeye Geç'}
            </button>
          )}
          {formHtml && (
            <div ref={formRef} style={{ marginTop: 16 }} />
          )}
        </div>
      </div>
    </Layout>
  );
}