import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, STATUSES } from '../../api/client';
import { OrderDetailView } from '../MyOrderDetail';

export default function AdminOrderDetail() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [status, setStatus] = useState('');
  const [msg, setMsg] = useState('');
  const [carriers, setCarriers] = useState([]);
  const [ship, setShip] = useState({ carrier: '', trackingNumber: '', estimatedDelivery: '' });
  const [note, setNote] = useState('');

  const load = () => api.getOrder(id).then((o) => {
    setOrder(o);
    setStatus(o.status);
    setNote(o.internalNote || '');
    const sh = o.shippingInfo || {};
    setShip({ carrier: sh.carrier || '', trackingNumber: sh.trackingNumber || '', estimatedDelivery: sh.estimatedDelivery || '' });
  }).catch(console.error);

  useEffect(() => { load(); api.getOrderCarriers().then(setCarriers).catch(console.error); }, [id]);

  const saveStatus = async () => {
    try {
      const updated = await api.setOrderStatus(id, status, '');
      setOrder(updated);
      setMsg('Durum güncellendi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  const saveShipping = async () => {
    try {
      const updated = await api.setOrderShipping(id, {
        carrier: ship.carrier,
        trackingNumber: ship.trackingNumber,
        estimatedDelivery: ship.estimatedDelivery || null,
      });
      setOrder(updated);
      setMsg('Kargo bilgisi kaydedildi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  const saveNote = async () => {
    try {
      const updated = await api.setOrderNote(id, note);
      setOrder(updated);
      setMsg('Not kaydedildi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  if (!order) return <p>Yükleniyor...</p>;

  const payment = order.payment || {};
  const allowedNext = STATUSES.filter((s) => {
    if (s === order.status) return true;
    // basit istemci filtresi; backend doğrular
    if ((s === 'Hazırlanıyor' || s === 'Kargoda') && payment.status !== 'Paid') return false;
    return true;
  });

  return (
    <>
      <p><Link to="/admin/orders">← Siparişler</Link></p>
      {msg && <div className="info">{msg}</div>}
      <OrderDetailView order={order} isAdmin />

      <div className="form" style={{ marginTop: 16 }}>
        <h3>Sipariş Durumu</h3>
        <div className="row">
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            {allowedNext.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <button className="btn" onClick={saveStatus}>Kaydet</button>
        </div>
      </div>

      <div className="form" style={{ marginTop: 16 }}>
        <h3>Kargo Formu</h3>
        <div className="row">
          <select value={ship.carrier} onChange={(e) => setShip({ ...ship, carrier: e.target.value })}>
            <option value="">Firma seçin</option>
            {carriers.map((c) => <option key={c.name} value={c.name}>{c.name}</option>)}
          </select>
          <input
            placeholder="Takip numarası"
            value={ship.trackingNumber}
            onChange={(e) => setShip({ ...ship, trackingNumber: e.target.value })}
          />
        </div>
        <div className="row" style={{ marginTop: 8 }}>
          <input
            type="date"
            value={ship.estimatedDelivery ? ship.estimatedDelivery.slice(0, 10) : ''}
            onChange={(e) => setShip({ ...ship, estimatedDelivery: e.target.value })}
          />
          <button className="btn" onClick={saveShipping}>Kargoya Ver</button>
        </div>
      </div>

      <div className="form" style={{ marginTop: 16 }}>
        <h3>İç Not</h3>
        <textarea value={note} onChange={(e) => setNote(e.target.value)} rows={3} style={{ width: '100%' }} />
        <button className="btn" onClick={saveNote} style={{ marginTop: 8 }}>Kaydet</button>
      </div>
    </>
  );
}