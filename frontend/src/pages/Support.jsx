import { useState } from 'react';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';

const SUPPORT_MAIL = 'info@shop.com';

export default function Support() {
  const { user } = useAuth();
  const [subject, setSubject] = useState('');
  const [message, setMessage] = useState('');
  const [sent, setSent] = useState(false);

  const send = (e) => {
    e.preventDefault();
    const body =
      `Ad Soyad: ${user?.name || '-'}\n` +
      `E-posta: ${user?.email || '-'}\n` +
      `Telefon: ${user?.phone || '-'}\n\n` +
      `Açıklama:\n${message}`;
    window.location.href =
      `mailto:${SUPPORT_MAIL}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    setSent(true);
  };

  return (
    <Layout>
      <h2>Destek</h2>
      <p className="muted">
        Talebin <strong>{SUPPORT_MAIL}</strong> adresine e-posta olarak iletilir.
        Gönder'e basınca mail uygulaman açılır.
      </p>
      {sent && <div className="info">Mail uygulamana yönlendirildin. Göndermeyi orada tamamla.</div>}
      <form onSubmit={send} className="form form-wrap">
        <label>Konu*<input value={subject} onChange={(e) => setSubject(e.target.value)} placeholder="Örn. Siparişim kargoya verilmedi" required /></label>
        <label>Açıklama*<textarea value={message} onChange={(e) => setMessage(e.target.value)} placeholder="Sorunu kısaca anlat..." required rows={6} /></label>
        <button className="btn" type="submit">Gönder</button>
      </form>
    </Layout>
  );
}
