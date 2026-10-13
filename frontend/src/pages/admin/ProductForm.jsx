import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../../api/client';

const empty = { name: '', description: '', price: '', stock: '', categoryId: '', brandId: '', productCode: '', barcode: '', vatRate: '20' };

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

  const loadImages = () => {
    if (!isNew) api.getProductImages(id).then(setImages).catch(console.error);
  };

  useEffect(() => {
    api.getCategories().then(setCategories).catch(console.error);
    api.getBrands().then(setBrands).catch(console.error);
    if (!isNew) {
      api.getProduct(id).then((p) => setForm({
        name: p.name, description: p.description, price: p.price, stock: p.stock,
        categoryId: p.categoryId, brandId: p.brandId || '',
        productCode: p.productCode || '', barcode: p.barcode || '', vatRate: p.vatRate ?? '20',
      })).catch(console.error);
      loadImages();
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
    if (!isNew && (id === 'undefined' || !/^\d+$/.test(id))) {
      setMsg('Geçersiz ürün id. Listeden bir ürüne tıklayarak düzenleyin.');
      return;
    }
    const payload = {
      name: form.name,
      description: form.description,
      price: Number(form.price),
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
      }
      navigate('/admin/products');
    } catch (err) {
      setMsg(err.message);
    }
  };

  const rows = isNew ? staged : images;

  return (
    <>
      <h2>{isNew ? 'Yeni Ürün' : `Ürün Düzenle #${id}`}</h2>
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
      {isNew && <p className="muted">Yeni üründe resimler Kaydet'e basınca ürüne işlenir.</p>}

      <div className="save-bar">
        <button className="hbtn" onClick={() => navigate('/admin/products')}>Vazgeç</button>
        <button className="btn" onClick={submit}>Kaydet</button>
      </div>
    </>
  );
}
