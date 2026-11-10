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

const grid2 = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 };

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
        <div style={grid2}>
          {FIELDS.map((f) => (
            <label key={f.key}>{f.label}
              <input value={values[f.key] || ''} onChange={(e) => setField(f.key, e.target.value)} />
            </label>
          ))}
        </div>

        <fieldset style={{ marginTop: 16, padding: 12, border: '1px solid #ddd', borderRadius: 8 }}>
          <legend><strong>Kargo Firma API Bilgileri</strong></legend>
          <div style={grid2}>
            {CARRIERS.map((c) => (
              <fieldset key={c.slug} style={{ padding: 12, border: '1px dashed #ccc', borderRadius: 8 }}>
                <legend><strong>{c.name}</strong></legend>
                <div style={grid2}>
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
                </div>
              </fieldset>
            ))}
          </div>
        </fieldset>

        <fieldset style={{ marginTop: 16, padding: 12, border: '1px solid #ddd', borderRadius: 8 }}>
          <legend><strong>iyzico Ödeme Ayarları</strong></legend>
          <div style={grid2}>
            <label>Ödeme Modu
              <select value={values.iyzicoMode || 'test'} onChange={(e) => setField('iyzicoMode', e.target.value)}>
                <option value="test">Test (Sandbox)</option>
                <option value="live">Canlı (Production)</option>
              </select>
            </label>
            <label>Kimlik No (Test TC, üretimde kullanıcıdan alınır)
              <input value={values.iyzicoIdentityNumber || '74300864791'} onChange={(e) => setField('iyzicoIdentityNumber', e.target.value)} />
            </label>
          </div>

          <div style={{ ...grid2, marginTop: 12 }}>
            <fieldset style={{ padding: 12, border: '1px dashed #ccc', borderRadius: 8 }}>
              <legend>Test (Sandbox) Bilgileri</legend>
              <div style={grid2}>
                <label>Base URL
                  <input value={values.iyzicoTestBaseUrl || 'https://sandbox-api.iyzipay.com'} onChange={(e) => setField('iyzicoTestBaseUrl', e.target.value)} placeholder="https://sandbox-api.iyzipay.com" />
                </label>
                <label>API Key
                  <input value={values.iyzicoTestApiKey || ''} onChange={(e) => setField('iyzicoTestApiKey', e.target.value)} />
                </label>
                <label>Secret Key
                  <input type="password" value={values.iyzicoTestSecretKey || ''} onChange={(e) => setField('iyzicoTestSecretKey', e.target.value)} />
                </label>
              </div>
            </fieldset>

            <fieldset style={{ padding: 12, border: '1px dashed #ccc', borderRadius: 8 }}>
              <legend>Canlı (Production) Bilgileri</legend>
              <div style={grid2}>
                <label>Base URL
                  <input value={values.iyzicoLiveBaseUrl || 'https://api.iyzipay.com'} onChange={(e) => setField('iyzicoLiveBaseUrl', e.target.value)} placeholder="https://api.iyzipay.com" />
                </label>
                <label>API Key
                  <input value={values.iyzicoLiveApiKey || ''} onChange={(e) => setField('iyzicoLiveApiKey', e.target.value)} />
                </label>
                <label>Secret Key
                  <input type="password" value={values.iyzicoLiveSecretKey || ''} onChange={(e) => setField('iyzicoLiveSecretKey', e.target.value)} />
                </label>
              </div>
            </fieldset>
          </div>
        </fieldset>
      </form>
      <div className="save-bar">
        <button className="btn" onClick={save}>Kaydet</button>
      </div>
    </>
  );
}