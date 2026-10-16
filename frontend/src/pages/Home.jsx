import { useEffect, useState } from 'react';
import { api } from '../api/client';
import Layout from '../components/Layout';
import Sidebar from '../components/Sidebar';
import ProductCard from '../components/ProductCard';
import Pagination from '../components/Pagination';

const PAGE_SIZE = 20;

export default function Home() {
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [selected, setSelected] = useState(null);
  const [q, setQ] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getCategories().then(setCategories).catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    const t = setTimeout(() => {
      api.getProducts(selected, q.trim() || undefined, page, PAGE_SIZE)
        .then((res) => {
          setProducts(res.items || []);
          setTotal(res.total || 0);
          setTotalPages(res.totalPages || 1);
        })
        .catch(console.error)
        .finally(() => setLoading(false));
    }, 300);
    return () => clearTimeout(t);
  }, [selected, q, page]);

  const goPage = (n) => {
    setPage(n);
    window.scrollTo(0, 0);
  };

  return (
    <Layout sidebar={<Sidebar categories={categories} selected={selected} onSelect={(id) => { setSelected(id); setPage(1); }} />}>
      <h2>Anasayfa - Ürünler</h2>
      <input
        className="search-input"
        value={q}
        onChange={(e) => { setQ(e.target.value); setPage(1); }}
        placeholder="Ürün adı, kodu veya barkod ile ara..."
      />
      {loading ? <p>Yükleniyor...</p> : (
        products.length === 0 ? <p>Ürün bulunamadı.</p> : (
          <>
            <p className="muted">{total} ürün bulundu</p>
            <div className="grid">
              {products.map((p) => <ProductCard key={p.id} product={p} />)}
            </div>
            <Pagination page={page} totalPages={totalPages} onChange={goPage} />
          </>
        )
      )}
    </Layout>
  );
}
