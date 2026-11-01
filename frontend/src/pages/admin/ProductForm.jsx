import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../../api/client';

const empty = { name: '', description: '', price: '', listPrice: '', stock: '', categoryId: '', brandId: '', productCode: '', barcode: '', vatRate: '20' };

export default function ProductForm() {
  const { id } = useParams();
  const isNew = !id || id === 'new';
  const navigate = useNavigate();
  const [categories, setCategories] = useState([]);
  const [brands, setBrands] = useState([]);
  const [form, setForm] = useState(empty);
  const [images, setImages] = useState([]);
  const [staged, setStaged] = useState([]);
  const [urlInput, setUrlInput] = useState('');
  const [msg, setMsg] = useState('');
  const [uploading, setUploading] = useState(false);
  const [similarItems, setSimilarItems] = useState([]);
  const [similarSearch, setSimilarSearch] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [pickerProducts, setPickerProducts] = useState([]);
  const [pickerSelected, setPickerSelected] = useState(() => new Set());
  const [bidirectional, setBidirectional] = useState(true);
  const SIMILAR_MAX = 12;
  const MIN_DISPLAY = 6;

  const loadImages = () => {
    if (!isNew) api.getProductImages(id).then(setImages).catch(console.error);
  };

  useEffect(() => {
    api.getCategories().then(setCategories).catch(console.error);
    api.getBrands().then(setBrands).catch(console.error);
    if (!isNew) {
      api.getProduct(id).then((p) => setForm({
        name: p.name, description: p.description, price: p.price, listPrice: p.listPrice ? p.listPrice : '', stock: p.stock,
        categoryId: p.categoryId, brandId: p.brandId || '',
        productCode: p.productCode || '', barcode: p.barcode || '', vatRate: p.vatRate ?? '20',
      })).catch(console.error);
      loadImages();
      api.getAdminSimilarProducts(id).then(setSimilarItems).catch(console.error);
    }
  }, [id]);

  const onFile = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      if (isNew) {
        const url = await api.uploadImage(file);
        setStaged((s) => [...s, { id: `yeni-${Date.now()}`, url }]);
      } else {
        const img = await api.uploadProductImage(id, file);
        setImages((imgs) => [...imgs, img]);
      }
    } catch (err) {
      setMsg(err.message);
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  };

  const addUrl = async () => {
    if (!urlInput.trim()) return;
    try {
      if (isNew) {
        setStaged((s) => [...s, { id: `yeni-${Date.now()}`, url: urlInput.trim() }]);
      } else {
        const img = await api.addProductImage(id, urlInput.trim());
        setImages((imgs) => [...imgs, img]);
      }
      setUrlInput('');
    } catch (err) {
      setMsg(err.message);
    }
  };

  const removeImage = async (img) => {
    if (String(img.id).startsWith('yeni-')) {
      setStaged((s) => s.filter((x) => x.id !== img.id));
      return;
    }
    try {
      await api.deleteProductImage(img.id);
      setImages((imgs) => imgs.filter((x) => x.id !== img.id));
    } catch (err) {
      setMsg(err.message);
    }
  };

  const submit = async (e) => {
    e.preventDefault();
    setMsg('');
    if (form.listPrice !== '' && Number(form.listPrice) > 0 && Number(form.listPrice) < Number(form.price)) {
      setMsg('Liste fiyatı normal fiyattan düşük olamaz.');
      return;
    }
    if (!isNew && (id === 'undefined' || !/^\d+$/.test(id))) {
      setMsg('Geçersiz ürün id. Listeden bir ürüne tıklayarak düzenleyin.');
      return;
    }
    const payload = {
      name: form.name,
      description: form.description,
      price: Number(form.price),
      listPrice: form.listPrice === '' ? null : Number(form.listPrice),
      stock: Number(form.stock),
      categoryId: Number(form.categoryId),
      brandId: form.brandId ? Number(form.brandId) : null,
      productCode: form.productCode,
      barcode: form.barcode,
      vatRate: Number(form.vatRate),
    };
    try {
      if (isNew) {
        const created = await api.createProduct(payload);
        for (const s of staged) {
          await api.addProductImage(created.id, s.url);
        }
      } else {
        await api.updateProduct(id, payload);
        // Benzer ürünleri tek istekte güncelle (sıralı + karşılıklı seçenek).
        await api.updateAdminSimilarProducts(id, similarItems.map((x) => x.id), bidirectional);
      }
      // Kaydet sonrası ürün düzenleme sayfasında kal; benzer listesini yeniden yükle.
      api.getAdminSimilarProducts(id).then(setSimilarItems).catch(console.error);
      setMsg('Kaydedildi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  // Listeden seç paneli: açıldığında veya arama değiştiğinde ürünleri yükle.
  useEffect(() => {
    if (!pickerOpen || isNew) return;
    const load = similarSearch.trim()
      ? api.searchAdminProducts(similarSearch.trim(), id)
      : api.getProducts(undefined, '', 1, 100).then((res) => (Array.isArray(res) ? res : (res.items || [])));
    Promise.resolve(load)
      .then((items) => {
        const arr = Array.isArray(items) ? items : (items.items || []);
        const selected = new Set(similarItems.map((x) => x.id));
        setPickerProducts(arr.filter((p) => p.id !== Number(id) && !selected.has(p.id)));
      })
      .catch(console.error);
  }, [pickerOpen, similarSearch, similarItems, id, isNew]);

  // Debounce'lu benzer ürün arama.
  useEffect(() => {
    if (isNew) return;
    if (!similarSearch.trim()) { setSearchResults([]); return; }
    const t = setTimeout(() => {
      api.searchAdminProducts(similarSearch.trim(), id)
        .then((res) => {
          const selected = new Set(similarItems.map((x) => x.id));
          setSearchResults((res || []).filter((r) => !selected.has(r.id)));
        })
        .catch(console.error);
    }, 300);
    return () => clearTimeout(t);
  }, [similarSearch, similarItems, id, isNew]);

  const addSimilar = (prod) => {
    if (similarItems.length >= SIMILAR_MAX) {
      setMsg(`Manuel benzer ürün sayısı en fazla ${SIMILAR_MAX} olabilir.`);
      return;
    }
    setSimilarItems((prev) => [...prev, prod]);
    setSearchResults((prev) => prev.filter((x) => x.id !== prod.id));
    setSimilarSearch('');
  };

  const removeSimilar = (pid) => {
    setSimilarItems((prev) => prev.filter((x) => x.id !== pid));
  };

  const moveSimilar = (index, dir) => {
    setSimilarItems((prev) => {
      const next = [...prev];
      const target = index + dir;
      if (target < 0 || target >= next.length) return prev;
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  };

  const togglePickerItem = (pid, checked) => {
    setPickerSelected((prev) => {
      const next = new Set(prev);
      if (checked) next.add(pid); else next.delete(pid);
      return next;
    });
  };

  const addPicked = () => {
    const selectedProducts = pickerProducts.filter((p) => pickerSelected.has(p.id));
    setSimilarItems((prev) => {
      const remaining = SIMILAR_MAX - prev.length;
      const toAdd = selectedProducts.slice(0, remaining);
      return [...prev, ...toAdd];
    });
    setPickerSelected(new Set());
    setPickerOpen(false);
  };

  const rows = isNew ? staged : images;

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>{isNew ? 'Yeni Ürün' : `Ürün Düzenle #${id}`}</h2>
        {!isNew && (
          <button className="btn" type="button" onClick={() => window.open(`/product/${id}`, '_blank', 'noopener,noreferrer')}>
            Ürüne Git
          </button>
        )}
      </div>
      {msg && <div className="error">{msg}</div>}
      <form onSubmit={submit} className="form">
        <div className="row2">
          <label>Ürün adı*<input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required /></label>
          <label>Marka
            <select value={form.brandId} onChange={(e) => setForm({ ...form, brandId: e.target.value })}>
              <option value="">Markasız</option>
              {brands.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
            </select>
          </label>
        </div>
        <div className="row2">
          <label>Ürün Kodu<input value={form.productCode} onChange={(e) => setForm({ ...form, productCode: e.target.value })} placeholder="ELK-001" /></label>
          <label>Barkod<input value={form.barcode} onChange={(e) => setForm({ ...form, barcode: e.target.value })} placeholder="8690..." /></label>
        </div>
        <div className="row2">
          <label>Kategori*
            <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })} required>
              <option value="">Seç</option>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </label>
          <label>Adet*<input type="number" min="0" value={form.stock} onChange={(e) => setForm({ ...form, stock: e.target.value })} required /></label>
        </div>
        <div className="row2">
          <label>Fiyat (₺)*<input type="number" step="0.01" min="0" value={form.price} onChange={(e) => setForm({ ...form, price: e.target.value })} required /></label>
          <label>Liste Fiyatı (₺)<input type="number" step="0.01" min="0" value={form.listPrice} onChange={(e) => setForm({ ...form, listPrice: e.target.value })} /></label>
        </div>
        <div className="row2">
          <label>KDV (%)*<input type="number" step="0.01" min="0" max="100" value={form.vatRate} onChange={(e) => setForm({ ...form, vatRate: e.target.value })} required /></label>
        </div>
        <label>Açıklama<textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
      </form>

      <h3>Resimler ({rows.length})</h3>
      <div className="form">
        <div className="row">
          <input value={urlInput} onChange={(e) => setUrlInput(e.target.value)} placeholder="https://... resim URL" style={{ flex: 1 }} />
          <button type="button" className="btn" onClick={addUrl}>+ URL Ekle</button>
        </div>
        <label>Dosyadan yükle (jpg/png/webp, max 5MB)<input type="file" accept="image/*" onChange={onFile} /></label>
        {uploading && <span className="muted">Yükleniyor...</span>}
      </div>
      {rows.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr><th>ID</th><th>Resim</th><th>URL</th><th></th></tr>
            </thead>
            <tbody>
              {rows.map((img) => (
                <tr key={img.id}>
                  <td>{img.id}</td>
                  <td><img className="timg" src={api.imageUrl(img.url)} alt="" /></td>
                  <td className="url-cell">{img.url}</td>
                  <td className="right"><button className="hbtn" onClick={() => removeImage(img)}>Sil</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {isNew && <p className="muted">Yeni üründe resimler Kaydet&apos;e basınca ürüne işlenir.</p>}

      {!isNew && (
        <section className="form" style={{ marginTop: 16 }}>
          <h3>Benzer Ürünler ({similarItems.length}/{SIMILAR_MAX})</h3>
          <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
            <input
              value={similarSearch}
              onChange={(e) => setSimilarSearch(e.target.value)}
              placeholder="Ürün ara (ad, kod, barkod)..."
              disabled={similarItems.length >= SIMILAR_MAX}
              style={{ flex: 1 }}
            />
            <button
              type="button"
              className="btn"
              disabled={similarItems.length >= SIMILAR_MAX}
              onClick={() => {
                if (pickerOpen && pickerSelected.size > 0) {
                  addPicked();
                } else {
                  setPickerOpen((o) => !o);
                }
              }}
            >
              {pickerOpen
                ? (pickerSelected.size > 0 ? `Seçilenleri Ekle (${pickerSelected.size})` : 'Kapat')
                : 'Listeden seç'}
            </button>
          </div>
          {pickerOpen && (
            <div className="table-wrap" style={{ marginBottom: 8, maxHeight: 320, overflow: 'auto' }}>
              <table className="table">
                <thead>
                  <tr><th></th><th>Ürün</th><th>Fiyat</th><th>Stok</th></tr>
                </thead>
                <tbody>
                  {pickerProducts
                    .filter((p) => {
                      const q = similarSearch.trim().toLowerCase();
                      if (!q) return true;
                      return (
                        String(p.name || '').toLowerCase().includes(q) ||
                        String(p.productCode || '').toLowerCase().includes(q) ||
                        String(p.barcode || '').toLowerCase().includes(q) ||
                        String(p.id).includes(q)
                      );
                    })
                    .map((p) => (
                    <tr
                      key={p.id}
                      onClick={() => togglePickerItem(p.id, !pickerSelected.has(p.id))}
                      style={{
                        cursor: 'pointer',
                        background: pickerSelected.has(p.id) ? '#dceeff' : undefined,
                      }}
                    >
                      <td>
                        <input
                          type="checkbox"
                          checked={pickerSelected.has(p.id)}
                          onChange={(e) => togglePickerItem(p.id, e.target.checked)}
                          onClick={(e) => e.stopPropagation()}
                        />
                      </td>
                      <td><img className="timg" src={api.imageUrl(p.imageUrl || p.image) || api.primaryImage(p)} alt="" /> #{p.id} {p.name}</td>
                      <td>{Number(p.price).toLocaleString('tr-TR')} ₺</td>
                      <td>{p.stock}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <div style={{ padding: 8 }}>
                <button type="button" className="btn" onClick={addPicked} disabled={pickerSelected.size === 0}>Seçilenleri ekle ({pickerSelected.size})</button>
              </div>
            </div>
          )}
          {similarItems.length >= SIMILAR_MAX && (
            <p className="muted">Manuel limit doldu; yeni ürün eklenemez.</p>
          )}
          {/* Arama sonuçları da aynı çoklu seç panelinde checkbox ile seçilebilir. */}
          {similarItems.length > 0 && (
            <div className="table-wrap">
              <table className="table">
                <tbody>
                  {similarItems.map((it, idx) => (
                    <tr key={it.id}>
                      <td><img className="timg" src={api.imageUrl(it.imageUrl || it.image) || api.primaryImage(it)} alt="" /></td>
                      <td>#{it.id} {it.name}</td>
                      <td>{Number(it.price).toLocaleString('tr-TR')} ₺</td>
                      <td>
                        <button type="button" className="hbtn" onClick={() => moveSimilar(idx, -1)} disabled={idx === 0}>↑</button>
                        <button type="button" className="hbtn" onClick={() => moveSimilar(idx, 1)} disabled={idx === similarItems.length - 1}>↓</button>
                        <button type="button" className="hbtn" onClick={() => removeSimilar(it.id)}>Kaldır</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 8 }}>
            <input type="checkbox" checked={bidirectional} onChange={(e) => setBidirectional(e.target.checked)} />
            Karşılıklı ekle/kaldır
          </label>
          <p className="muted">Seçilen ürünlerin benzer listesine bu ürün de eklenecek.</p>
          <p className="muted">Manuel eklenen ürün sayısı {MIN_DISPLAY}&apos;den azsa müşteriye aynı kategorideki ürünler otomatik gösterilir.</p>
        </section>
      )}

      <div className="save-bar">
        <button className="hbtn" onClick={() => navigate('/admin/products')}>Vazgeç</button>
        <button className="btn" onClick={submit}>Kaydet</button>
      </div>
    </>
  );
}
