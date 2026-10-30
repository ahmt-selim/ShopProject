import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { useCart } from '../store/CartContext';

export default function PaymentResult() {
  const { id } = useParams();
  const { clear } = useCart();
  const [order, setOrder] = useState(null);
  const [status, setStatus] = useState('checking');
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    let tries = 0;
    const load = async () => {
      try {
        const o = await api.getMyOrder(id);
        if (cancelled) return;
        setOrder(o);
        const ps = (o.paymentStatus || '').toLowerCase();
        if (ps === 'paid') {
          setStatus('paid');
          clear();
        } else if (ps === 'failed') {
          setStatus('failed');
        } else if (tries < 5) {
          tries += 1;
          setTimeout(load, 2000);
        } else {
          setStatus('pending');
        }
      } catch (e) {
        if (!cancelled) setError(e.message);
      }
    };
    load();
    return () => { cancelled = true; };
  }, [id, clear]);

  return (
    <Layout>
      <h2>Ödeme Sonucu</h2>
      {error && <div className="error">{error}</div>}
      {status === 'checking' && <p>Ödeme sonucu kontrol ediliyor...</p>}
      {status === 'paid' && order && (
        <div>
          <p className="ok">Ödemeniz başarıyla alındı.</p>
          <p>Sipariş No: <strong>{order.orderNo || order.id}</strong></p>
          <p>Toplam: <strong>{Number(order.total).toLocaleString('tr-TR')} ₺</strong></p>
          <Link className="btn" to="/account/orders">Siparişlerime Git</Link>
        </div>
      )}
      {status === 'failed' && (
        <div>
          <p className="error">Ödeme başarısız oldu. Tekrar deneyebilirsiniz.</p>
          <Link className="btn" to="/checkout">Tekrar Dene</Link>
        </div>
      )}
      {status === 'pending' && (
        <div>
          <p>Ödeme henüz onaylanmadı. Lütfen daha sonra kontrol edin.</p>
          <Link className="btn" to="/account/orders">Siparişlerime Git</Link>
        </div>
      )}
    </Layout>
  );
}