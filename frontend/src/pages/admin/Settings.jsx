import { useEffect, useState } from 'react';
import { api } from '../../api/client';

const FIELDS = [
  { key: 'storeName', label: 'Mağaza Adı' },
  { key: 'announcement', label: 'Duyuru Metni (boşsa gösterilmez)' },
  { key: 'currency', label: 'Para Birimi' },
  { key: 'vatRate', label: 'KDV Oranı (%)' },
  { key: 'freeShippingLimit', label: 'Ücretsiz Kargo Limiti (₺)' },
  { key: 'shippingFee', label: 'Kargo Ücreti (₺)' },
  { key: 'supportEmail', label: 'Destek E-postası' },
  { key: 'supportPhone', label: 'Destek Telefonu' },
  { key: 'storeAddress', label: 'Mağaza Adresi' },
  { key: 'maintenanceMode', label: 'Bakım Modu (true/false)' },
];

const CARRIERS = [
  { name: 'Yurtiçi Kargo', slug: 'yurtici' },
  { name: 'Aras Kargo', slug: 'aras' },
  { name: 'MNG Kargo', slug: 'mng' },
  { name: 'PTT Kargo', slug: 'ptt' },
  { name: 'Sürat Kargo', slug: 'surat' },
];

export default function AdminSettings() {
  const [values, setValues] = useState({});
  const [msg, setMsg] = useState('');

  useEffect(() => {
    api.getSettings().then((list) => {
      const map = {};
      list.forEach((s) => (map[s.key] = s.value));
      setValues(map);
    }).catch(console.error);
  }, []);

  const save = async (e) => {
    e.preventDefault();
    setMsg('');
    try {
      await api.saveSettings(values);
      setMsg('Ayarlar kaydedildi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  const setField = (key, value) => setValues((prev) => ({ ...prev, [key]: value }));

  return (
    <>
      <h2>Ayarlar</h2>
      {msg && <div className="info">{msg}</div>}
      <form onSubmit={save} className="form">
        {FIELDS.map((f) => (
          <label key={f.key}>{f.label}
            <input value={values[f.key] || ''} onChange={(e) => setField(f.key, e.target.value)} />
          </label>
        ))}

        {CARRIERS.map((c) => (
          <fieldset key={c.slug} style={{ marginTop: 16, padding: 12, border: '1px solid #ddd', borderRadius: 8 }}>
            <legend><strong>{c.name} API Bilgileri</strong></legend>
            <label>Base URL
              <input value={values[`${c.slug}ApiBaseUrl`] || ''} onChange={(e) => setField(`${c.slug}ApiBaseUrl`, e.target.value)} />
            </label>
            <label>API Key
              <input value={values[`${c.slug}ApiKey`] || ''} onChange={(e) => setField(`${c.slug}ApiKey`, e.target.value)} />
            </label>
            <label>API Secret
              <input type="password" value={values[`${c.slug}ApiSecret`] || ''} onChange={(e) => setField(`${c.slug}ApiSecret`, e.target.value)} />
            </label>
            <label>Kullanıcı Adı
              <input value={values[`${c.slug}ApiUsername`] || ''} onChange={(e) => setField(`${c.slug}ApiUsername`, e.target.value)} />
            </label>
            <label>Şifre
              <input type="password" value={values[`${c.slug}ApiPassword`] || ''} onChange={(e) => setField(`${c.slug}ApiPassword`, e.target.value)} />
            </label>
          </fieldset>
        ))}
      </form>
      <div className="save-bar">
        <button className="btn" onClick={save}>Kaydet</button>
      </div>
    </>
  );
}