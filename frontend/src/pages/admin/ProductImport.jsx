import { useEffect, useState } from 'react';
import { api } from '../../api/client';

const FIELDS = [
  { key: 'name', label: 'Ürün Adı*', hint: 'Zorunlu' },
  { key: 'description', label: 'Açıklama' },
  { key: 'price', label: 'Fiyat*', hint: '99.99 formatı' },
  { key: 'productCode', label: 'Ürün Kodu' },
  { key: 'barcode', label: 'Barkod' },
  { key: 'vatRate', label: 'KDV', hint: '0-100' },
  { key: 'category', label: 'Kategori', hint: 'Adı ya da id' },
  { key: 'brand', label: 'Marka', hint: 'Adı ya da id' },
  { key: 'stock', label: 'Adet' },
  { key: 'imageUrls', label: 'Resimler', hint: 'Virgülle ayırın' },
];

const defaultMap = { name: 'A', description: 'B', price: 'C', productCode: 'D', barcode: 'E', vatRate: 'F', category: 'G', brand: 'H', stock: 'I', imageUrls: 'J' };

export default function ProductImport() {
  const [templates, setTemplates] = useState([]);
  const [selected, setSelected] = useState(null);
  const [templateName, setTemplateName] = useState('');
  const [map, setMap] = useState(defaultMap);
  const [startRow, setStartRow] = useState(2);
  const [file, setFile] = useState(null);
  const [msg, setMsg] = useState('');
  const [result, setResult] = useState(null);
  const [busy, setBusy] = useState(false);

  const load = () => api.getImportTemplates().then(setTemplates).catch(console.error);
  useEffect(() => { load(); }, []);

  const pick = (t) => {
    setSelected(t.id);
    setTemplateName(t.name);
    setMap({ ...defaultMap, ...t.mappings });
    setStartRow(t.startRow || 2);
  };

  const newTemplate = () => {
    setSelected(null);
    setTemplateName('');
    setMap(defaultMap);
    setStartRow(2);
  };

  const save = async () => {
    setMsg('');
    if (!templateName.trim()) {
      setMsg('Şablon adı girin.');
      return;
    }
    try {
      if (selected) await api.updateImportTemplate(selected, templateName, map, Number(startRow));
      else {
        const created = await api.saveImportTemplate(templateName, map, Number(startRow));
        setSelected(created.id);
      }
      setMsg('Şablon kaydedildi.');
      load();
    } catch (err) {
      setMsg(err.message);
    }
  };

  const remove = async (id) => {
    if (!confirm('Şablon silinsin mi?')) return;
    await api.deleteImportTemplate(id).catch((e) => setMsg(e.message));
    if (selected === id) newTemplate();
    load();
  };

  const doImport = async () => {
    setMsg('');
    setResult(null);
    if (!file) {
      setMsg('Excel dosyası seçin.');
      return;
    }
    setBusy(true);
    try {
      const res = await api.importProducts(file, {
        templateId: selected || undefined,
        mappings: selected ? undefined : map,
        startRow: Number(startRow),
      });
      setResult(res);
    } catch (err) {
      setMsg(err.message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <h2>Excel ile Toplu Ürün Ekleme</h2>
      {msg && <div className="error">{msg}</div>}
      <div className="account-grid">
        <div>
          <h3>Kayıtlı Şablonlar ({templates.length})</h3>
          {templates.length === 0 && <p className="muted">Henüz şablon yok. Sağdan eşleşmeyi yapıp kaydedin.</p>}
          {templates.map((t) => (
            <div key={t.id} className={`addr-card click ${selected === t.id ? 'sel' : ''}`} onClick={() => pick(t)}>
              <strong>{t.name}</strong>
              <div className="muted">Başlangıç satırı: {t.startRow}</div>
              <div className="row">
                <button className="hbtn" onClick={(e) => { e.stopPropagation(); remove(t.id); }}>Sil</button>
              </div>
            </div>
          ))}
          <button className="hbtn" onClick={newTemplate}>+ Yeni Şablon</button>
        </div>
        <div>
          <div className="form">
            <h3>Sütun Eşleşmesi</h3>
            <label>Şablon Adı<input value={templateName} onChange={(e) => setTemplateName(e.target.value)} placeholder="Örn. Standart" /></label>
            <label>Veri Başlangıç Satırı<input type="number" min="1" value={startRow} onChange={(e) => setStartRow(e.target.value)} /></label>
            <div className="map-grid">
              {FIELDS.map((f) => (
                <label key={f.key} className="map-cell">{f.label}
                  <input
                    value={map[f.key] || ''}
                    onChange={(e) => setMap({ ...map, [f.key]: e.target.value.toUpperCase() })}
                    placeholder="A"
                    maxLength={3}
                  />
                  {f.hint && <span className="muted">{f.hint}</span>}
                </label>
              ))}
            </div>
            <label>Excel Dosyası (.xlsx)<input type="file" accept=".xlsx,.xls" onChange={(e) => setFile(e.target.files?.[0] || null)} /></label>
            <div className="row">
              <button className="hbtn primary" onClick={save}>Kaydet</button>
              <button className="btn" onClick={doImport} disabled={busy}>{busy ? 'Aktarılıyor...' : 'İçeri Aktar'}</button>
            </div>
          </div>
          {result && (
            <div className="info" style={{ marginTop: 12 }}>
              <strong>{result.imported} ürün aktarıldı</strong>
              {result.skipped > 0 && `, ${result.skipped} satır atlandı:`}
              {result.errors?.map((e) => <div key={e.row}>Satır {e.row}: {e.error}</div>)}
            </div>
          )}
        </div>
      </div>
    </>
  );
}
