import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../../api/client';
import Pagination from '../../components/Pagination';

// Yeni ekleme yöntemi buraya tek satırla eklenir
const ADD_METHODS = [
  { id: 'manuel', label: 'Manuel', to: '/admin/products/new' },
  { id: 'excel', label: 'Excel', to: '/admin/products/import' },
];

const PAGE_SIZE = 20;

export default function AdminProducts() {
  const [products, setProducts] = useState([]);
  const [q, setQ] = useState(() => {
    try {
      const saved = JSON.parse(sessionStorage.getItem('adminProductsState') || 'null');
      return saved?.q || '';
    } catch {
      return '';
    }
  });
  const [page, setPage] = useState(() => {
    try {
      const saved = JSON.parse(sessionStorage.getItem('adminProductsState') || 'null');
      return saved?.page || 1;
    } catch {
      return 1;
    }
  });
  const [totalPages, setTotalPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [menuOpen, setMenuOpen] = useState(false);
  const [pageSize, setPageSize] = useState(20);
  const [selected, setSelected] = useState(() => new Set());
  const menuRef = useRef(null);
  const navigate = useNavigate();

  const toggleOne = (id, checked) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (checked) next.add(id); else next.delete(id);
      return next;
    });
  };
  const toggleAll = (checked) => {
    setSelected((prev) => {
      const next = new Set(prev);
      products.forEach((p) => { if (checked) next.add(p.id); else next.delete(p.id); });
      return next;
    });
  };
  const allSelected = products.length > 0 && products.every((p) => selected.has(p.id));

  const fetchProducts = (p, query) => {
    const size = pageSize;
    const handle = (res, isPaged) => {
      setSelected(new Set());
      if (isPaged) {
        setProducts(res.items || []);
        setTotal(res.total || 0);
        setTotalPages(res.totalPages || 1);
      } else {
        const items = Array.isArray(res) ? res : (res.items || []);
        setProducts(items);
        setTotal(items.length);
        setTotalPages(1);
      }
      try {
        const saved = JSON.parse(sessionStorage.getItem('adminProductsState') || 'null');
        if (saved && saved.page === p && (saved.q || '') === (query || '')) {
          const y = saved.scrollY || 0;
          requestAnimationFrame(() => {
            requestAnimationFrame(() => {
              window.scrollTo(0, y);
            });
          });
          sessionStorage.removeItem('adminProductsState');
        }
      } catch {
        // ignore
      }
    };
    if (size === 'all') {
      return api.getProducts(undefined, query || undefined)
        .then((res) => handle(res, false))
        .catch(console.error);
    }
    return api.getProducts(undefined, query || undefined, p, Number(size))
      .then((res) => handle(res, true))
      .catch(console.error);
  };

  useEffect(() => {
    if ('scrollRestoration' in window.history) window.history.scrollRestoration = 'manual';
  }, []);
  useEffect(() => {
    const t = setTimeout(() => {
      fetchProducts(page, q.trim());
    }, 300);
    return () => clearTimeout(t);
  }, [q, page, pageSize]);

  useEffect(() => {
    const close = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
    };
    const esc = (e) => {
      if (e.key === 'Escape') setMenuOpen(false);
    };
    document.addEventListener('mousedown', close);
    document.addEventListener('keydown', esc);
    return () => {
      document.removeEventListener('mousedown', close);
      document.removeEventListener('keydown', esc);
    };
  }, []);

  const load = () => fetchProducts(page, q.trim());

  const bulkDelete = async () => {
    if (selected.size === 0) return;
    if (!confirm(`${selected.size} ürün silinsin mi?`)) return;
    try {
      await Promise.all([...selected].map((id) => api.deleteProduct(id)));
      setSelected(new Set());
      load();
    } catch (err) {
      alert(err?.message || 'Silme sırasında hata oluştu.');
    }
  };

  return (
    <>
      <div className="page-head">
        <h2>Ürünler ({total})</h2>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {selected.size > 0 && (
            <select
              className="admin-select"
              defaultValue=""
              onChange={(e) => {
                const v = e.target.value;
                e.target.value = '';
                if (v === 'delete') bulkDelete();
              }}
            >
              <option value="" disabled>Toplu İşlemler ({selected.size})</option>
              <option value="delete">Seçili ürünleri sil</option>
            </select>
          )}
          <div className="menu-wrap" ref={menuRef}>
          <button className="btn" onClick={() => setMenuOpen((o) => !o)}>+ Ürün Ekle</button>
          {menuOpen && (
            <div className="dropdown">
              {ADD_METHODS.map((m) => (
                <button key={m.id} onClick={() => navigate(m.to)}>{m.label}</button>
              ))}
            </div>
          )}
        </div>
        </div>
      </div>
      <input
        className="search-input"
        value={q}
        onChange={(e) => { setQ(e.target.value); setPage(1); }}
        placeholder="Ürün adı, kodu veya barkod ile ara..."
      />
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, margin: '8px 0' }}>
        <label htmlFor="pageSize">Sayfada gösterilecek ürün sayısı:</label>
        <select
          id="pageSize"
          className="admin-select"
          value={pageSize}
          onChange={(e) => {
            const v = e.target.value;
            setPageSize(v === 'all' ? 'all' : Number(v));
            setPage(1);
          }}
        >
          <option value={20}>20</option>
          <option value={50}>50</option>
          <option value={100}>100</option>
          <option value="all">Hepsi</option>
        </select>
      </div>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr><th><input type="checkbox" checked={allSelected} onChange={(e) => toggleAll(e.target.checked)} onClick={(e) => e.stopPropagation()} /></th><th></th><th className="col-product">Ürün</th><th>Ürün Kodu</th><th>Barkod</th><th>KDV</th><th>Fiyat</th><th>Stok</th><th></th></tr>
          </thead>
          <tbody>
            {products.map((p) => (
              <tr key={p.id} className="click" onClick={() => { try { sessionStorage.setItem('adminProductsState', JSON.stringify({ page, q, scrollY: window.scrollY })); } catch { /* ignore */ } navigate(`/admin/products/${p.id}`); }}>
                <td onClick={(e) => e.stopPropagation()}><input type="checkbox" checked={selected.has(p.id)} onChange={(e) => toggleOne(p.id, e.target.checked)} /></td>
                <td><img className="timg" src={api.primaryImage(p)} alt="" /></td>
                <td className="col-product"><strong>#{p.id} {p.name}</strong><br /><span className="muted">{p.categoryName}{p.brandName ? ` - ${p.brandName}` : ''}</span></td>
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
      <Pagination page={page} totalPages={totalPages} onChange={(n) => { setPage(n); window.scrollTo(0, 0); }} />
    </>
  );
}
