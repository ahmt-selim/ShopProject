import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, statusClass } from '../../api/client';
import Pagination from '../../components/Pagination';

const PAGE_SIZE = 20;

export default function AdminOrders() {
  const [orders, setOrders] = useState([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [total, setTotal] = useState(0);
  const navigate = useNavigate();

  useEffect(() => {
    api.getOrders(page, PAGE_SIZE)
      .then((res) => {
        setOrders(res.items || []);
        setTotal(res.total || 0);
        setTotalPages(res.totalPages || 1);
      })
      .catch(console.error);
  }, [page]);

  return (
    <>
      <h2>Siparişler ({total})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Sipariş No</th><th>Ad Soyad</th><th>Adres</th><th>Adet</th><th>Toplam</th><th>Durum</th></tr>
          </thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.id} className="click" onClick={() => navigate(`/admin/orders/${o.id}`)}>
                <td><strong>{o.orderNo}</strong><br /><span className="muted">{new Date(o.createdAt).toLocaleString('tr-TR')}</span></td>
                <td>{o.customerName}</td>
                <td>{o.address.addressCity}/{o.address.addressDistrict}<br /><span className="muted">{o.address.addressDetail?.slice(0, 40)}</span></td>
                <td>{o.itemsCount}</td>
                <td>{Number(o.total).toLocaleString('tr-TR')} ₺</td>
                <td><span className={`badge ${statusClass(o.status)}`}>{o.status}</span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {orders.length === 0 && <p className="muted">Henüz sipariş yok.</p>}
      <Pagination page={page} totalPages={totalPages} onChange={(n) => { setPage(n); window.scrollTo(0, 0); }} />
    </>
  );
}
