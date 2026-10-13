import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, statusClass } from '../../api/client';

export default function AdminCustomerDetail() {
  const { id } = useParams();
  const [data, setData] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    api.getCustomer(id).then(setData).catch(console.error);
  }, [id]);

  if (!data) return <p>Yükleniyor...</p>;
  const { user, addresses, orders } = data;

  return (
    <>
      <p><Link to="/admin/customers">← Müşteriler</Link></p>
      <h2>{user.name || user.email}</h2>
      <div className="account-grid">
        <div>
          <h3>Müşteri Bilgileri</h3>
          <div className="addr-card">
            <div><strong>E-posta:</strong> {user.email}</div>
            <div><strong>Telefon:</strong> {user.phone || '-'}</div>
            <div><strong>Rol:</strong> {user.role}</div>
          </div>
          <h3>Adresleri ({addresses.length})</h3>
          {addresses.map((a) => (
            <div key={a.id} className="addr-card">
              <strong>{a.title} - {a.city}/{a.district}</strong>
              <div>{a.fullName} - {a.phone}</div>
              <div className="muted">{a.detail}</div>
            </div>
          ))}
        </div>
        <div>
          <h3>Siparişleri ({orders.length})</h3>
          {orders.length === 0 && <p className="muted">Sipariş yok.</p>}
          {orders.map((o) => (
            <div key={o.id} className="addr-card click" onClick={() => navigate(`/admin/orders/${o.id}`)}>
              <strong>{o.orderNo}</strong> - {Number(o.total).toLocaleString('tr-TR')} ₺ ({o.itemsCount} ürün)
              <div><span className={`badge ${statusClass(o.status)}`}>{o.status}</span> <span className="muted">{new Date(o.createdAt).toLocaleString('tr-TR')}</span></div>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}
