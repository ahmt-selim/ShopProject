import { useEffect, useState } from 'react';
import { api } from '../api/client';
import Layout from '../components/Layout';
import Sidebar from '../components/Sidebar';
import ProductCard from '../components/ProductCard';

export default function Home() {
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [selected, setSelected] = useState(null);
  const [q, setQ] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getCategories().then(setCategories).catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    const t = setTimeout(() => {
      api.getProducts(selected, q.trim() || undefined)
        .then(setProducts)
        .catch(console.error)
        .finally(() => setLoading(false));
    }, 300);
    return () => clearTimeout(t);
  }, [selected, q]);

  return (
    <Layout sidebar={<Sidebar categories={categories} selected={selected} onSelect={setSelected} />}>
      <h2>Anasayfa - Ürünler</h2>
      <input
        className="search-input"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Ürün adı, kodu veya barkod ile ara..."
      />
      {loading ? <p>Yükleniyor...</p> : (
        products.length === 0 ? <p>Ürün bulunamadı.</p> : (
          <div className="grid">
            {products.map((p) => <ProductCard key={p.id} product={p} />)}
          </div>
        )
      )}
    </Layout>
  );
}
