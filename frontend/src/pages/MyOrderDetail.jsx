import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, statusClass } from '../api/client';
import { formatMoney, formatDateTime } from '../utils/format';
import Layout from '../components/Layout';
import OrderReviewBox from '../components/reviews/OrderReviewBox';

export function OrderDetailView({ order, showReviews, isAdmin }) {
  const delivered = order.status === 'Teslim Edildi';
  const payment = order.payment || {};
  const shipping = order.shippingInfo || {};
  const history = order.history || [];

  const itemsCount = order.items?.length ?? 0;
  const totalQty = order.items?.reduce((s, i) => s + (i.qty || 0), 0) ?? 0;

  return (
    <>
      <div className="page-head">
        <h2>{order.orderNo}</h2>
        <span className={`badge ${statusClass(order.status)}`}>{order.status}</span>
      </div>
      <p className="muted">{formatDateTime(order.createdAt)}</p>

      <div className="account-grid">
        <div>
          <h3>Ürünler ({itemsCount} kalem · {totalQty} adet)</h3>
          <div className="cart-list">
            {order.items.map((i) => (
              <div key={i.id} className="cart-item">
                <img src={api.imageUrl(i.imageUrl)} alt={i.productName} />
                <div className="cart-info">
                  <strong>
                    {i.productId ? (
                      <Link to={`/products/${i.productId}`}>{i.productName}</Link>
                    ) : (
                      i.productName
                    )}
                  </strong>
                  <div className="muted">{i.qty} x {formatMoney(i.price)}</div>
                  {showReviews && (
                    <OrderReviewBox productId={i.productId} productName={i.productName} delivered={delivered} />
                  )}
                </div>
                <strong>{formatMoney(i.qty * i.price)}</strong>
              </div>
            ))}
          </div>

          <div className="totals">
            <p>Ara toplam: {formatMoney(order.subtotal)}</p>
            <p>Kargo: {formatMoney(order.shipping)}</p>
            {order.discount ? <p>İndirim: -{formatMoney(order.discount)}</p> : null}
            {order.tax ? <p>KDV: {formatMoney(order.tax)}</p> : null}
            <h3>Toplam: {formatMoney(order.total)}</h3>
          </div>

          {/* Ödeme kartı */}
          <div className="addr-card" style={{ marginTop: 16 }}>
            <h3>Ödeme</h3>
            <div>
              <span className={`badge ${payment.status === 'Paid' ? 'ok' : payment.status === 'Failed' ? 'iptal' : 'hazir'}`}>
                {payment.status === 'Paid' ? 'Ödendi' : payment.status === 'Failed' ? 'Başarısız' : 'Ödeme bekleniyor'}
              </span>
            </div>
            {payment.cardLast4 ? (
              <div className="muted">
                {payment.cardAssociation || payment.cardFamily} •••• {payment.cardLast4}
              </div>
            ) : null}
            <div className="muted">
              {payment.installmentCount && payment.installmentCount > 1
                ? `${payment.installmentCount} taksit`
                : 'Tek çekim'}
            </div>
            {payment.paidAt ? <div className="muted">Ödeme tarihi: {formatDateTime(payment.paidAt)}</div> : null}
            {isAdmin && payment.paymentId ? (
              <div className="muted">
                PaymentId: {payment.paymentId}
                <button className="btn btn-sm" onClick={() => navigator.clipboard.writeText(payment.paymentId)}>Kopyala</button>
              </div>
            ) : null}
            {!isAdmin && payment.status !== 'Paid' ? (
              <div style={{ marginTop: 8 }}>
                <Link className="btn" to={`/account/orders/${order.id}/pay`}>Ödemeyi tamamla / Tekrar dene</Link>
              </div>
            ) : null}
          </div>

          {/* Kargo kartı */}
          <div className="addr-card" style={{ marginTop: 16 }}>
            <h3>Kargo</h3>
            {shipping.trackingNumber ? (
              <>
                {shipping.carrier ? <div>{shipping.carrier}</div> : null}
                <div className="muted">
                  Takip no: {shipping.trackingNumber}
                  <button className="btn btn-sm" onClick={() => navigator.clipboard.writeText(shipping.trackingNumber)}>Kopyala</button>
                </div>
                {shipping.trackingUrl ? (
                  <div><a href={shipping.trackingUrl} target="_blank" rel="noreferrer">Takip et</a></div>
                ) : null}
                {shipping.shippedAt ? <div className="muted">Gönderim: {formatDateTime(shipping.shippedAt)}</div> : null}
                {shipping.deliveredAt ? <div className="muted">Teslim: {formatDateTime(shipping.deliveredAt)}</div> : null}
              </>
            ) : (
              <div className="muted">Siparişiniz hazırlanıyor, kargoya verilince takip numarası burada görünecek.</div>
            )}
          </div>

          {/* Zaman çizelgesi */}
          <div className="addr-card" style={{ marginTop: 16 }}>
            <h3>Durum geçmişi</h3>
            {history.length === 0 ? (
              <div className="muted">Oluşturulma: {formatDateTime(order.createdAt)}</div>
            ) : (
              <ul className="timeline">
                {history.map((h, idx) => (
                  <li key={idx}>
                    <strong>{h.toStatus || h.to_status}</strong>
                    <span className="muted"> — {formatDateTime(h.changedAt || h.changed_at)}</span>
                    {isAdmin && h.changedByUserId ? <span className="muted"> ({h.changedByUserId})</span> : null}
                    {isAdmin && h.note ? <div className="muted">{h.note}</div> : null}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div>
          <h3>Teslimat Adresi</h3>
          <div className="addr-card">
            <strong>{order.address.addressTitle} - {order.address.addressCity}/{order.address.addressDistrict}</strong>
            <div>{order.address.addressFullName} - {order.address.addressPhone}</div>
            <div className="muted">{order.address.addressDetail}</div>
          </div>

          <h3>{isAdmin ? 'Müşteri' : 'İletişim bilgileri'}</h3>
          <div className="addr-card">
            <div>{order.customerName}</div>
            <div className="muted">{order.customerEmail} - {order.customerPhone}</div>
            {isAdmin ? (
              <div style={{ marginTop: 8 }}>
                <Link to={`/admin/customers/${order.userId}`}>Müşteri detayı</Link>
              </div>
            ) : null}
          </div>

          {isAdmin && order.internalNote ? (
            <div className="addr-card" style={{ marginTop: 16 }}>
              <h3>İç not</h3>
              <div>{order.internalNote}</div>
            </div>
          ) : null}
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
      <OrderDetailView order={order} showReviews />
    </Layout>
  );
}