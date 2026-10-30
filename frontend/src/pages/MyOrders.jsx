import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, statusClass } from '../api/client';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';

export default function MyOrders() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [orders, setOrders] = useState([]);

  useEffect(() => { api.getMyOrders().then(setOrders).catch(console.error); }, []);

  if (!user) return <Layout><p>Giriş yapman gerek.</p></Layout>;

  return (
    <Layout>
      <h2>Siparişlerim</h2>
      {orders.length === 0 ? (
        <p>Henüz siparişin yok.</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>Sipariş No</th><th>Tarih</th><th>Adet</th><th>Toplam</th><th>Durum</th><th>Ödeme</th></tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id} className="click" onClick={() => navigate(`/account/orders/${o.id}`)}>
                  <td>{o.orderNo}</td>
                  <td>{new Date(o.createdAt).toLocaleString('tr-TR')}</td>
                  <td>{o.itemsCount}</td>
                  <td>{Number(o.total).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ₺</td>
                  <td><span className={`badge ${statusClass(o.status)}`}>{o.status}</span></td>
                  <td><span className={`badge ${o.payment?.status === 'Paid' ? 'ok' : 'hazir'}`}>{o.payment?.status === 'Paid' ? 'Ödendi' : 'Bekliyor'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Layout>
  );
}
