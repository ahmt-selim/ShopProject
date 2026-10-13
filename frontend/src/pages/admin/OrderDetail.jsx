import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, STATUSES } from '../../api/client';
import { OrderDetailView } from '../MyOrderDetail';

export default function AdminOrderDetail() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [status, setStatus] = useState('');
  const [msg, setMsg] = useState('');

  const load = () => api.getOrder(id).then((o) => { setOrder(o); setStatus(o.status); }).catch(console.error);
  useEffect(() => { load(); }, [id]);

  const save = async () => {
    try {
      const updated = await api.setOrderStatus(id, status);
      setOrder(updated);
      setMsg('Durum güncellendi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  if (!order) return <p>Yükleniyor...</p>;

  return (
    <>
      <p><Link to="/admin/orders">← Siparişler</Link></p>
      {msg && <div className="info">{msg}</div>}
      <OrderDetailView order={order} />
      <div className="form" style={{ marginTop: 16 }}>
        <h3>Sipariş Durumu</h3>
        <div className="row">
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <button className="btn" onClick={save}>Kaydet</button>
        </div>
      </div>
    </>
  );
}
