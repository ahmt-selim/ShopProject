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

  return (
    <>
      <h2>Ayarlar</h2>
      {msg && <div className="info">{msg}</div>}
      <form onSubmit={save} className="form">
        {FIELDS.map((f) => (
          <label key={f.key}>{f.label}
            <input value={values[f.key] || ''} onChange={(e) => setValues({ ...values, [f.key]: e.target.value })} />
          </label>
        ))}
      </form>
      <div className="save-bar">
        <button className="btn" onClick={save}>Kaydet</button>
      </div>
    </>
  );
}
