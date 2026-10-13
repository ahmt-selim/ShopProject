import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';

export default function AdminProducts() {
  const [products, setProducts] = useState([]);
  const [q, setQ] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    const t = setTimeout(() => {
      api.getProducts(undefined, q.trim() || undefined).then(setProducts).catch(console.error);
    }, 300);
    return () => clearTimeout(t);
  }, [q]);

  const load = () => api.getProducts(undefined, q.trim() || undefined).then(setProducts).catch(console.error);

  return (
    <>
      <div className="page-head">
        <h2>Ürünler ({products.length})</h2>
        <button className="btn" onClick={() => navigate('/admin/products/new')}>+ Ürün Ekle</button>
      </div>
      <input
        className="search-input"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Ürün adı, kodu veya barkod ile ara..."
      />
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th></th><th>Ürün</th><th>Ürün Kodu</th><th>Barkod</th><th>KDV</th><th>Fiyat</th><th>Stok</th><th></th></tr>
          </thead>
          <tbody>
            {products.map((p) => (
              <tr key={p.id} className="click" onClick={() => navigate(`/admin/products/${p.id}`)}>
                <td><img className="timg" src={api.primaryImage(p)} alt="" /></td>
                <td><strong>#{p.id} {p.name}</strong><br /><span className="muted">{p.categoryName}{p.brandName ? ` - ${p.brandName}` : ''}</span></td>
                <td>{p.productCode || '-'}</td>
                <td>{p.barcode || '-'}</td>
                <td>%{p.vatRate}</td>
                <td>{Number(p.price).toLocaleString('tr-TR')} ₺</td>
                <td>{p.stock}</td>
                <td><button className="hbtn" onClick={(e) => { e.stopPropagation(); if (confirm('Silinsin mi?')) api.deleteProduct(p.id).then(load); }}>Sil</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="muted">Düzenlemek için satıra tıklayın.</p>
    </>
  );
}
