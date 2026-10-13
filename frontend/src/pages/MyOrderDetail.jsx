import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, statusClass } from '../api/client';
import Layout from '../components/Layout';

export function OrderDetailView({ order }) {
  return (
    <>
      <div className="page-head">
        <h2>{order.orderNo}</h2>
        <span className={`badge ${statusClass(order.status)}`}>{order.status}</span>
      </div>
      <p className="muted">{new Date(order.createdAt).toLocaleString('tr-TR')}</p>
      <div className="account-grid">
        <div>
          <h3>Ürünler ({order.itemsCount})</h3>
          <div className="cart-list">
            {order.items.map((i) => (
              <div key={i.id} className="cart-item">
                <img src={api.imageUrl(i.imageUrl)} alt={i.productName} />
                <div className="cart-info">
                  <strong>{i.productName}</strong>
                  <div className="muted">{i.qty} x {Number(i.price).toLocaleString('tr-TR')} ₺</div>
                </div>
                <strong>{(i.qty * i.price).toLocaleString('tr-TR')} ₺</strong>
              </div>
            ))}
          </div>
          <p>Ara toplam: {Number(order.subtotal).toLocaleString('tr-TR')} ₺</p>
          <p>Kargo: {Number(order.shipping).toLocaleString('tr-TR')} ₺</p>
          <h3>Toplam: {Number(order.total).toLocaleString('tr-TR')} ₺</h3>
        </div>
        <div>
          <h3>Teslimat Adresi</h3>
          <div className="addr-card">
            <strong>{order.address.addressTitle} - {order.address.addressCity}/{order.address.addressDistrict}</strong>
            <div>{order.address.addressFullName} - {order.address.addressPhone}</div>
            <div className="muted">{order.address.addressDetail}</div>
          </div>
          <h3>Müşteri</h3>
          <div className="addr-card">
            <div>{order.customerName}</div>
            <div className="muted">{order.customerEmail} - {order.customerPhone}</div>
          </div>
        </div>
      </div>
    </>
  );
}

export default function MyOrderDetail() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);

  useEffect(() => {
    api.getMyOrder(id).then(setOrder).catch(console.error);
  }, [id]);

  if (!order) return <Layout><p>Yükleniyor...</p></Layout>;

  return (
    <Layout>
      <p><Link to="/account/orders">← Siparişlerim</Link></p>
      <OrderDetailView order={order} />
    </Layout>
  );
}
