import { useEffect, useState } from 'react';
import { api } from '../api/client';
import Layout from '../components/Layout';
import { IconEye, IconEyeOff } from '../components/icons';
import { useAuth } from '../store/AuthContext';

const MIN_PW = 8;
const MAX_PW = 128;

export default function Profile() {
  const { user, setUser } = useAuth();
  const [profile, setProfile] = useState({ name: '', phone: '', email: '' });
  const [msg, setMsg] = useState('');

  // Şifre değiştirme dialogu
  const [pwOpen, setPwOpen] = useState(false);
  const [pw, setPw] = useState({ current: '', next: '', confirm: '' });
  const [show, setShow] = useState({ current: false, next: false, confirm: false });
  const [pwMsg, setPwMsg] = useState('');
  const [pwLoading, setPwLoading] = useState(false);

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

  const openPw = () => {
    setPw({ current: '', next: '', confirm: '' });
    setShow({ current: false, next: false, confirm: false });
    setPwMsg('');
    setPwLoading(false);
    setPwOpen(true);
  };

  const submitPw = async (e) => {
    e.preventDefault();
    setPwMsg('');
    if (!pw.current || !pw.next || !pw.confirm) {
      setPwMsg('Tüm alanlar zorunlu.');
      return;
    }
    if (pw.next.length < MIN_PW) {
      setPwMsg(`Şifre en az ${MIN_PW} karakter olmalı.`);
      return;
    }
    if (pw.next.length > MAX_PW) {
      setPwMsg(`Şifre en fazla ${MAX_PW} karakter olmalı.`);
      return;
    }
    if (pw.next !== pw.confirm) {
      setPwMsg('Yeni şifreler eşleşmiyor.');
      return;
    }
    if (pw.next === pw.current) {
      setPwMsg('Yeni şifre mevcut şifreyle aynı olamaz.');
      return;
    }
    setPwLoading(true);
    try {
      const data = await api.changePassword({
        currentPassword: pw.current,
        newPassword: pw.next,
        confirmNewPassword: pw.confirm,
      });
      if (data?.token) {
        localStorage.setItem('token', data.token);
        if (data.user) setUser(data.user);
      }
      setPwOpen(false);
      setMsg('Şifreniz değiştirildi.');
    } catch (err) {
      setPwMsg(err.message);
    } finally {
      setPwLoading(false);
    }
  };

  if (!user) return <Layout><p>Giriş yapman gerek.</p></Layout>;

  const toggleShow = (key) => setShow((s) => ({ ...s, [key]: !s[key] }));

  return (
    <Layout>
      <h2>Hesap Bilgileri</h2>
      {msg && <div className="info">{msg}</div>}
      <form onSubmit={save} className="form form-wrap">
        <label>Ad Soyad<input value={profile.name} onChange={(e) => setProfile({ ...profile, name: e.target.value })} /></label>
        <label>Telefon<input value={profile.phone} onChange={(e) => setProfile({ ...profile, phone: e.target.value })} placeholder="05xx..." /></label>
        <label>E-posta<input type="email" value={profile.email} onChange={(e) => setProfile({ ...profile, email: e.target.value })} /></label>
        <label>
          Şifre
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <input type="password" value="••••••••" disabled style={{ flex: 1 }} />
            <button type="button" className="btn" onClick={openPw}>Şifreyi değiştir</button>
          </div>
        </label>
        <label>Rol<input value={user.role} disabled /></label>
        <button className="btn" type="submit">Kaydet</button>
      </form>

      {pwOpen && (
        <div className="lightbox" onClick={() => { if (!pwLoading) setPwOpen(false); }} role="dialog" aria-modal="true" aria-label="Şifre değiştir">
          <div style={{ background: 'var(--surface)', padding: 16, borderRadius: 8, maxWidth: 420, width: '90%' }} onClick={(e) => e.stopPropagation()}>
            <h3>Şifre değiştir</h3>
            {pwMsg && <div className="info">{pwMsg}</div>}
            <form onSubmit={submitPw} className="form">
              <label>
                Mevcut şifre
                <span className="pw-wrap">
                  <input
                    type={show.current ? 'text' : 'password'}
                    value={pw.current}
                    onChange={(e) => setPw({ ...pw, current: e.target.value })}
                    autoComplete="current-password"
                  />
                  <button type="button" className="pw-toggle" onClick={() => toggleShow('current')} title={show.current ? 'Gizle' : 'Göster'} aria-label={show.current ? 'Şifreyi gizle' : 'Şifreyi göster'}>
                    {show.current ? <IconEyeOff /> : <IconEye />}
                  </button>
                </span>
              </label>
              <label>
                Yeni şifre
                <span className="pw-wrap">
                  <input
                    type={show.next ? 'text' : 'password'}
                    value={pw.next}
                    onChange={(e) => setPw({ ...pw, next: e.target.value })}
                    autoComplete="new-password"
                  />
                  <button type="button" className="pw-toggle" onClick={() => toggleShow('next')} title={show.next ? 'Gizle' : 'Göster'} aria-label={show.next ? 'Şifreyi gizle' : 'Şifreyi göster'}>
                    {show.next ? <IconEyeOff /> : <IconEye />}
                  </button>
                </span>
              </label>
              <label>
                Yeni şifre (tekrar)
                <span className="pw-wrap">
                  <input
                    type={show.confirm ? 'text' : 'password'}
                    value={pw.confirm}
                    onChange={(e) => setPw({ ...pw, confirm: e.target.value })}
                    autoComplete="new-password"
                  />
                  <button type="button" className="pw-toggle" onClick={() => toggleShow('confirm')} title={show.confirm ? 'Gizle' : 'Göster'} aria-label={show.confirm ? 'Şifreyi gizle' : 'Şifreyi göster'}>
                    {show.confirm ? <IconEyeOff /> : <IconEye />}
                  </button>
                </span>
              </label>
              <div style={{ display: 'flex', gap: 8 }}>
                <button className="btn" type="submit" disabled={pwLoading}>
                  {pwLoading ? 'Gönderiliyor...' : 'Kaydet'}
                </button>
                <button type="button" className="btn" onClick={() => setPwOpen(false)} disabled={pwLoading}>
                  İptal
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </Layout>
  );
}