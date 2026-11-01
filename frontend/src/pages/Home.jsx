import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import Layout from '../components/Layout';
import Sidebar from '../components/Sidebar';
import ProductCard from '../components/ProductCard';
import Pagination from '../components/Pagination';

function computePageSize() {
  if (typeof window === 'undefined') return 20;
  const cols = Math.max(1, Math.floor((window.innerWidth - 260) / 236));
  const rows = Math.max(3, Math.ceil((window.innerHeight - 200) / 280));
  const size = cols * rows;
  return Math.min(60, Math.max(12, size));
}

export default function Home() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [selected, setSelected] = useState(() => {
    const c = Number(searchParams.get('category'));
    return c > 0 ? c : null;
  });
  const [q, setQ] = useState('');
  const [sort, setSort] = useState('varsayilan');
  const [pageSize, setPageSize] = useState(() => computePageSize());
  const [page, setPage] = useState(() => {
    const p = Number(searchParams.get('page'));
    return p > 0 ? p : 1;
  });
  const [totalPages, setTotalPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getCategories().then(setCategories).catch(console.error);
  }, []);

  useEffect(() => {
    let t;
    const onResize = () => {
      clearTimeout(t);
      t = setTimeout(() => {
        const next = computePageSize();
        setPageSize((prev) => {
          if (prev !== next) {
            setPage(1);
            return next;
          }
          return prev;
        });
      }, 200);
    };
    window.addEventListener('resize', onResize);
    return () => {
      clearTimeout(t);
      window.removeEventListener('resize', onResize);
    };
  }, []);

  useEffect(() => {
    if ('scrollRestoration' in window.history) {
      window.history.scrollRestoration = 'manual';
    }
  }, []);

  // Footer'daki kategori linkleri (/?category=ID) ile senkron kal.
  useEffect(() => {
    const c = Number(searchParams.get('category'));
    setSelected(c > 0 ? c : null);
    const p = Number(searchParams.get('page'));
    setPage(p > 0 ? p : 1);
  }, [searchParams]);

  useEffect(() => {
    setLoading(true);
    const t = setTimeout(() => {
      api.getProducts(selected, q.trim() || undefined, page, pageSize, sort)
        .then((res) => {
          setProducts(res.items || []);
          setTotal(res.total || 0);
          setTotalPages(res.totalPages || 1);
        })
        .catch(console.error)
        .finally(() => {
          setLoading(false);
          try {
            const saved = JSON.parse(sessionStorage.getItem('homeListState') || 'null');
            if (saved && saved.page === page) {
              const y = saved.scrollY || 0;
              requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                  window.scrollTo(0, y);
                });
              });
              sessionStorage.removeItem('homeListState');
            }
          } catch {
            // ignore
          }
        });
    }, 300);
    return () => clearTimeout(t);
  }, [selected, q, page, sort, pageSize]);

  useEffect(() => {
    const save = () => {
      try {
        sessionStorage.setItem('homeListState', JSON.stringify({ page, scrollY: window.scrollY }));
      } catch {
        // ignore
      }
    };
    const onClick = (e) => {
      const a = e.target && e.target.closest ? e.target.closest('a[href^="/product/"]') : null;
      if (a) save();
    };
    document.addEventListener('click', onClick, true);
    return () => document.removeEventListener('click', onClick, true);
  }, [page]);

  const goPage = (n) => {
    setPage(n);
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (n > 1) next.set('page', String(n));
      else next.delete('page');
      return next;
    });
    window.scrollTo(0, 0);
  };

  const selectCategory = (id) => {
    setSelected(id);
    setPage(1);
    setSearchParams(id ? { category: String(id) } : {});
  };

  return (
    <Layout
      sidebar={<Sidebar categories={categories} selected={selected} onSelect={selectCategory} />}
    >
      <h2>Anasayfa - Ürünler</h2>
      <input
        className="search-input"
        value={q}
        onChange={(e) => { setQ(e.target.value); setPage(1); }}
        placeholder="Ürün adı, kodu veya barkod ile ara..."
      />
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 14 }}>
        <select
          className="admin-select"
          value={sort}
          onChange={(e) => { setSort(e.target.value); setPage(1); }}
          aria-label="Sıralama"
        >
          <option value="varsayilan">Varsayılan</option>
          <option value="bestseller">En çok satan</option>
          <option value="priceAsc">Fiyat: Düşükten Yükseğe</option>
          <option value="priceDesc">Fiyat: Yüksekten Düşüğe</option>
          <option value="nameAsc">İsim: A-Z</option>
          <option value="nameDesc">İsim: Z-A</option>
        </select>
      </div>
      {loading ? <div className="page-spinner"><span className="spinner" /></div> : (
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
