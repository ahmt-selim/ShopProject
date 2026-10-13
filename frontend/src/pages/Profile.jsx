import { useEffect, useState } from 'react';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';

export default function Profile() {
  const { user, setUser } = useAuth();
  const [profile, setProfile] = useState({ name: '', phone: '', email: '' });
  const [msg, setMsg] = useState('');

  useEffect(() => {
    if (user) setProfile({ name: user.name || '', phone: user.phone || '', email: user.email || '' });
  }, [user]);

  const save = async (e) => {
    e.preventDefault();
    setMsg('');
    try {
      const updated = await api.updateProfile(profile);
      setUser(updated);
      setMsg('Hesap bilgileri kaydedildi.');
    } catch (err) {
      setMsg(err.message);
    }
  };

  if (!user) return <Layout><p>Giriş yapman gerek.</p></Layout>;

  return (
    <Layout>
      <h2>Hesap Bilgileri</h2>
      {msg && <div className="info">{msg}</div>}
      <form onSubmit={save} className="form form-wrap">
        <label>Ad Soyad<input value={profile.name} onChange={(e) => setProfile({ ...profile, name: e.target.value })} /></label>
        <label>Telefon<input value={profile.phone} onChange={(e) => setProfile({ ...profile, phone: e.target.value })} placeholder="05xx..." /></label>
        <label>E-posta<input type="email" value={profile.email} onChange={(e) => setProfile({ ...profile, email: e.target.value })} /></label>
        <label>Rol<input value={user.role} disabled /></label>
        <button className="btn" type="submit">Kaydet</button>
      </form>
    </Layout>
  );
}
