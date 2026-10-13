import { useEffect, useState } from 'react';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';

const emptyAddr = { title: '', fullName: '', phone: '', city: '', district: '', detail: '' };

export default function Addresses() {
  const { user } = useAuth();
  const [addresses, setAddresses] = useState([]);
  const [form, setForm] = useState(emptyAddr);
  const [editing, setEditing] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [msg, setMsg] = useState('');

  const loadAddr = () => api.getAddresses().then(setAddresses).catch(console.error);
  useEffect(() => { loadAddr(); }, []);

  const saveAddress = async (e) => {
    e.preventDefault();
    try {
      if (editing) await api.updateAddress(editing, form);
      else await api.addAddress(form);
      setForm(emptyAddr);
      setEditing(null);
      setShowForm(false);
      loadAddr();
    } catch (err) {
      setMsg(err.message);
    }
  };

  const edit = (a) => {
    setEditing(a.id);
    setForm({ title: a.title, fullName: a.fullName, phone: a.phone, city: a.city, district: a.district, detail: a.detail });
    setShowForm(true);
  };

  if (!user) return <Layout><p>Giriş yapman gerek.</p></Layout>;

  return (
    <Layout>
      <div className="page-head">
        <h2>Adreslerim ({addresses.length})</h2>
        <button className="btn" onClick={() => { setEditing(null); setForm(emptyAddr); setShowForm(!showForm); }}>+ Yeni Adres</button>
      </div>
      {msg && <div className="error">{msg}</div>}
      {showForm && (
        <form onSubmit={saveAddress} className="form form-wrap">
          <label>Başlık (Ev/İş)<input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder="Ev" /></label>
          <label>Ad Soyad<input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required /></label>
          <label>Telefon<input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} required /></label>
          <div className="row2">
            <label>Şehir<input value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} required /></label>
            <label>İlçe<input value={form.district} onChange={(e) => setForm({ ...form, district: e.target.value })} /></label>
          </div>
          <label>Adres Detayı<textarea value={form.detail} onChange={(e) => setForm({ ...form, detail: e.target.value })} required /></label>
          <div className="row">
            <button className="btn" type="submit">Kaydet</button>
            <button type="button" className="hbtn" onClick={() => { setShowForm(false); setEditing(null); }}>Vazgeç</button>
          </div>
        </form>
      )}
      {addresses.map((a) => (
        <div key={a.id} className="addr-card">
          <strong>{a.title || 'Adres'} - {a.city}/{a.district}</strong>
          <div>{a.fullName} - {a.phone}</div>
          <div className="muted">{a.detail}</div>
          <div className="row">
            <button className="hbtn" onClick={() => edit(a)}>Düzenle</button>
            <button className="hbtn" onClick={() => api.deleteAddress(a.id).then(loadAddr)}>Sil</button>
          </div>
        </div>
      ))}
    </Layout>
  );
}
