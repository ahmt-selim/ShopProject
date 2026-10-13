import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';

export default function AdminCustomers() {
  const [list, setList] = useState([]);
  const navigate = useNavigate();

  useEffect(() => { api.getCustomers().then(setList).catch(console.error); }, []);

  return (
    <>
      <h2>Müşteriler ({list.length})</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th>Ad Soyad</th><th>E-posta</th><th>Telefon</th><th>Rol</th><th>Sipariş</th><th>Harcama</th></tr>
          </thead>
          <tbody>
            {list.map((u) => (
              <tr key={u.id} className="click" onClick={() => navigate(`/admin/customers/${u.id}`)}>
                <td>{u.name || '-'}</td>
                <td>{u.email}</td>
                <td>{u.phone || '-'}</td>
                <td>{u.role}</td>
                <td>{u.orderCount}</td>
                <td>{Number(u.totalSpent).toLocaleString('tr-TR')} ₺</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
