import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api, statusClass } from '../api/client';
import Layout from '../components/Layout';

export default function OrderTrack() {
  const [orderNo, setOrderNo] = useState('');
  const [email, setEmail] = useState('');
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    setResult(null);
    if (!orderNo.trim() || !email.trim()) {
      setError('Sipariş numarası ve e-posta gerekli.');
      return;
    }
    setLoading(true);
    try {
      setResult(await api.trackOrder(orderNo.trim(), email.trim()));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <p className="breadcrumb">
        <Link to="/">Anasayfa</Link>
        <span aria-hidden="true">›</span>
        <span>Sipariş takibi</span>
      </p>
      <h2>Sipariş Takibi</h2>
      <p className="muted">Sipariş numaranız ve e-posta adresinizle siparişinizin durumunu görüntüleyin.</p>

      <form className="form form-wrap" onSubmit={submit}>
        <label>
          Sipariş No:
          <input
            value={orderNo}
            onChange={(e) => setOrderNo(e.target.value)}
            placeholder="Ör: SP-2026-0001"
            autoComplete="off"
          />
        </label>
        <label>
          E-posta
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
          />
        </label>
        {error && <p className="error">{error}</p>}
        <button type="submit" className="btn" disabled={loading}>
          {loading ? 'Sorgulanıyor...' : 'Siparişi Sorgula'}
        </button>
      </form>

      {result && (
        <section aria-live="polite">
          <div className="page-head">
            <h3>{result.orderNo}</h3>
            <span className={`badge ${statusClass(result.status)}`}>{result.status}</span>
          </div>
          <p className="muted">
            {new Date(result.createdAt).toLocaleString('tr-TR')}
            {result.city && ` · Teslimat ili: ${result.city}`}
          </p>
          <div className="cart-list">
            {result.items.map((i, idx) => (
              <div key={idx} className="cart-item">
                <img src={api.imageUrl(i.imageUrl)} alt={i.productName} />
                <div className="cart-info">
                  <strong>{i.productName}</strong>
                  <div className="muted">{i.qty} x {Number(i.price).toLocaleString('tr-TR')} ₺</div>
                </div>
                <strong>{(i.qty * i.price).toLocaleString('tr-TR')} ₺</strong>
              </div>
            ))}
          </div>
          <p>Ara toplam: {Number(result.subtotal).toLocaleString('tr-TR')} ₺</p>
          <p>Kargo: {Number(result.shipping).toLocaleString('tr-TR')} ₺</p>
          <h3>Toplam: {Number(result.total).toLocaleString('tr-TR')} ₺</h3>
        </section>
      )}

      <p className="muted">
        <Link to="/login">Üye girişi</Link> yaparak tüm siparişlerinizi tek ekrandan takip edebilirsiniz.
      </p>
    </Layout>
  );
}
