import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { useAuth } from '../store/AuthContext';
import { api } from '../api/client';

export default function VerifyEmail() {
  const { verifyEmail } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState(location.state?.email || '');
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [loading, setLoading] = useState(false);

  const resend = async () => {
    if (loading) return;
    setError('');
    setNotice('');
    setLoading(true);
    try {
      await api.resendVerification(email.trim());
      setNotice('Dogrulama kodu tekrar gonderildi.');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const submit = async (e) => {
    e.preventDefault();
    if (loading) return;
    setError('');
    setNotice('');
    const cleanCode = code.trim();
    if (!/^\d{6}$/.test(cleanCode)) {
      setError('6 haneli dogrulama kodunu girin.');
      return;
    }
    setLoading(true);
    try {
      const user = await verifyEmail(email.trim(), cleanCode);
      navigate(user.role === 'admin' ? '/admin/products' : '/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <div className="form-wrap">
        <h2>E-posta Dogrulama</h2>
        {notice && <div className="info" role="status" aria-live="polite">{notice}</div>}
        {error && <div className="error" role="alert" aria-live="assertive">{error}</div>}
        <form onSubmit={submit} className="form">
          <label>
            E-posta
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
          </label>
          <label>
            Dogrulama kodu
            <input
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
              required
              inputMode="numeric"
              autoComplete="one-time-code"
              placeholder="6 haneli kod"
            />
          </label>
          <button className="btn" type="submit" disabled={loading}>
            {loading ? 'Dogrulaniyor...' : 'Kodu Dogrula'}
          </button>
        </form>
        <p>
          <button type="button" className="btn" onClick={resend} disabled={loading}>
            Kodu tekrar gonder
          </button>
        </p>
        <p><Link to="/login">Giris sayfasina don</Link></p>
      </div>
    </Layout>
  );
}