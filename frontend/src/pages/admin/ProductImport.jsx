import { useEffect, useRef, useState } from 'react';
import { api } from '../../api/client';

const FIELDS = [
  { key: 'name', label: 'Ürün Adı' },
  { key: 'description', label: 'Açıklama' },
  { key: 'price', label: 'Fiyat', hint: '99.99 formatı' },
  { key: 'productCode', label: 'Ürün Kodu' },
  { key: 'barcode', label: 'Barkod' },
  { key: 'vatRate', label: 'KDV', hint: '0-100' },
  { key: 'category', label: 'Kategori' },
  { key: 'brand', label: 'Marka' },
  { key: 'stock', label: 'Adet' },
  { key: 'imageUrls', label: 'Resimler', hint: 'J veya J,K gibi', wide: true },
];

const MATCH_OPTIONS = [
  { value: 'productId', label: 'Ürün İd' },
  { value: 'productCode', label: 'Ürün Kodu' },
  { value: 'barcode', label: 'Barkod' },
];

const matchLabel = (v) => MATCH_OPTIONS.find((o) => o.value === v)?.label || v;

const defaultMap = { name: 'A', description: 'B', price: 'C', productCode: 'D', barcode: 'E', vatRate: 'F', category: 'G', brand: 'H', stock: 'I', imageUrls: 'J', productId: '' };

export default function ProductImport() {
  const [templates, setTemplates] = useState([]);
  const [selected, setSelected] = useState(null);
  const [templateName, setTemplateName] = useState('');
  const [map, setMap] = useState(defaultMap);
  const [matchBy, setMatchBy] = useState('productCode');
  const [startRow, setStartRow] = useState(2);
  const [file, setFile] = useState(null);
  const [msg, setMsg] = useState('');
  const [msgType, setMsgType] = useState('error');
  const [result, setResult] = useState(null);
  const [busy, setBusy] = useState(false);
  const fileInputRef = useRef(null);

  const clearFileInput = () => { if (fileInputRef.current) fileInputRef.current.value = ''; };

  const fail = (m) => { setMsgType('error'); setMsg(m); };
  const ok = (m) => { setMsgType('success'); setMsg(m); };

  const load = () => api.getImportTemplates().then(setTemplates).catch(console.error);
  useEffect(() => { load(); }, []);

  const selectedTemplate = templates.find((t) => t.id === selected) || null;

  const pick = (t) => {
    setSelected(t.id);
    setTemplateName(t.name);
    setMap({ ...defaultMap, ...t.mappings });
    setStartRow(t.startRow || 2);
    setMatchBy(t.matchBy || 'productCode');
    setFile(null);
    clearFileInput();
  };

  const newTemplate = () => {
    setSelected(null);
    setTemplateName('');
    setMap(defaultMap);
    setMatchBy('productCode');
    setStartRow(2);
    setFile(null);
    clearFileInput();
  };

  const checkMatchColumn = () => {
    if (!map[matchBy]?.trim()) {
      fail(`Eşleştirme alanı (${matchLabel(matchBy)}) için sütun harfi girin.`);
      return false;
    }
    return true;
  };

  const save = async () => {
    setMsg('');
    if (!templateName.trim()) {
      fail('Şablon adı girin.');
      return;
    }
    if (!checkMatchColumn()) return;
    const fileName = file?.name || selectedTemplate?.fileName || '';
    try {
      if (selected) await api.updateImportTemplate(selected, templateName, map, Number(startRow), matchBy, fileName);
      else {
        const created = await api.saveImportTemplate(templateName, map, Number(startRow), matchBy, fileName);
        setSelected(created.id);
      }
      ok('Şablon kaydedildi.');
      load();
    } catch (err) {
      fail(err.message);
    }
  };

  const remove = async (id) => {
    if (!confirm('Şablon silinsin mi?')) return;
    await api.deleteImportTemplate(id).catch((e) => fail(e.message));
    if (selected === id) newTemplate();
    load();
  };

  const doImport = async () => {
    setMsg('');
    setResult(null);
    if (!file && !selectedTemplate?.hasFile) {
      fail(selectedTemplate?.fileName
        ? 'Kayıtlı dosyanın kopyası sunucuda yok, lütfen dosyayı yeniden seçin.'
        : 'Excel dosyası seçin.');
      return;
    }
    if (!checkMatchColumn()) return;
    setBusy(true);
    try {
      const res = await api.importProducts(file, {
        templateId: selected || undefined,
        mappings: selected ? undefined : map,
        startRow: Number(startRow),
        matchBy,
      });
      setResult(res);
      load();
    } catch (err) {
      fail(err.message);
    } finally {
      setBusy(false);
    }
  };

  const resultHasError = result && ((result.skipped || 0) > 0 || (result.errors?.length || 0) > 0);

  return (
    <>
      <h2>Excel ile Toplu Ürün Ekleme</h2>
      {msg && <div className={msgType === 'success' ? 'success' : 'error'}>{msg}</div>}
      <div className="account-grid">
        <div>
          <h3>Kayıtlı Şablonlar ({templates.length})</h3>
          {templates.length === 0 && <p className="muted">Henüz şablon yok. Sağdan eşleşmeyi yapıp kaydedin.</p>}
          {templates.map((t) => (
            <div key={t.id} className={`addr-card click ${selected === t.id ? 'sel' : ''}`} onClick={() => pick(t)}>
              <strong>{t.name}</strong>
              <div className="muted">Başlangıç satırı: {t.startRow} • Eşleşme: {matchLabel(t.matchBy)}</div>
              {t.fileName && <div className="muted">Dosya: {t.fileName}</div>}
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
            <div className="map-grid" style={{ marginBottom: 10 }}>
              <label className="map-cell">Veri Başlangıç Satırı
                <input
                  type="number"
                  min="1"
                  step="1"
                  value={startRow}
                  onChange={(e) => {
                    const n = e.target.valueAsNumber;
                    setStartRow(Number.isNaN(n) ? '' : Math.trunc(n));
                  }}
                  onBlur={() => {
                    if (!startRow || Number(startRow) < 1) setStartRow(1);
                  }}
                />
              </label>
              <label className="map-cell">Ürün Eşleştirme
                <select value={matchBy} onChange={(e) => setMatchBy(e.target.value)} style={{ maxWidth: 90, width: '100%' }}>
                  {MATCH_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </label>
              <label className="map-cell">Ürün İd
                <input
                  value={map.productId || ''}
                  onChange={(e) => setMap({ ...map, productId: e.target.value.toUpperCase() })}
                  placeholder="A"
                  maxLength={3}
                />
              </label>
            </div>
            <div className="map-grid">
              {FIELDS.map((f) => (
                <label key={f.key} className="map-cell" style={f.wide ? { gridColumn: '1 / -1' } : undefined}>{f.label}
                  <input
                    value={map[f.key] || ''}
                    onChange={(e) => setMap({ ...map, [f.key]: e.target.value.toUpperCase() })}
                    placeholder="A"
                    {...(f.wide ? {} : { maxLength: 3 })}
                    style={f.wide ? { maxWidth: 'none', width: '100%' } : undefined}
                  />
                  {f.hint && <span className="muted">{f.hint}</span>}
                </label>
              ))}
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: 14 }}>
              <span>Excel Dosyası (.xlsx)</span>
              <input ref={fileInputRef} type="file" accept=".xlsx,.xls" style={{ display: 'none' }} onChange={(e) => setFile(e.target.files?.[0] || null)} />
              <div className="file-picker">
                <span className={`file-name${file?.name || selectedTemplate?.fileName ? '' : ' empty'}`}>
                  {file?.name || selectedTemplate?.fileName || 'Dosya seçilmedi'}
                </span>
                <button type="button" className="hbtn" onClick={() => fileInputRef.current?.click()}>
                  {file?.name || selectedTemplate?.fileName ? 'Değiştir' : 'Dosya Seç'}
                </button>
              </div>
            </div>
            <div className="row" style={{ justifyContent: 'space-between' }}>
              <button className="hbtn primary" onClick={save}>Kaydet</button>
              <button className="btn green" onClick={doImport} disabled={busy}>{busy ? 'Aktarılıyor...' : 'İçeri Aktar'}</button>
            </div>
          </div>
          {result && (
            <div className={resultHasError ? 'error' : 'success'} style={{ marginTop: 12 }}>
              <strong>{resultHasError ? 'Hata Oluştu!' : 'Ürün aktarımı tamamlandı.'}</strong>
              <div>{result.imported} ürün eklendi, {result.updated || 0} ürün güncellendi</div>
              {resultHasError && (
                <>
                  <div>{result.skipped} satır atlandı:</div>
                  {result.errors?.map((e) => <div key={e.row}>Satır {e.row}: {e.error}</div>)}
                </>
              )}
            </div>
          )}
        </div>
      </div>
    </>
  );
}
