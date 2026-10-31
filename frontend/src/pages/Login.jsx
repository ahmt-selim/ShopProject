import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { IconEye, IconEyeOff } from '../components/icons';
import { useAuth } from '../store/AuthContext';
import { api } from '../api/client';

/* eslint-disable react/prop-types */
export function PasswordField({ value, onChange, label }) {
  const [show, setShow] = useState(false);
  return (
    <label>{label || 'Sifre'}
      <span className="pw-wrap">
        <input type={show ? 'text' : 'password'} value={value} onChange={onChange} required />
        <button type="button" className="pw-toggle" onClick={() => setShow((s) => !s)} title={show ? 'Gizle' : 'Goster'} aria-label={show ? 'Sifreyi gizle' : 'Sifreyi goster'}>
          {show ? <IconEyeOff /> : <IconEye />}
        </button>
      </span>
    </label>
  );
}

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [resending, setResending] = useState(false);
  const [attempted, setAttempted] = useState(false);
  const [networkError, setNetworkError] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    setNotice('');
    setAttempted(false);
    setNetworkError(false);
    try {
      const user = await login(email, password);
      if (!user.emailConfirmed) {
        navigate('/verify-email', { state: { email: email.trim() } });
        return;
      }
      navigate(user.role === 'admin' ? '/admin/products' : '/');
    } catch (err) {
      const msg = err?.message || '';
      const isNet = err instanceof TypeError ||
        /failed to fetch|networkerror|network error|load failed/i.test(msg);
      setNetworkError(isNet);
      setError(msg);
      setAttempted(true);
    }
  };

  const resend = async () => {
    if (resending) return;
    setError('');
    setNotice('');
    setResending(true);
    try {
      await api.resendVerification(email.trim());
      setNotice('Dogrulama kodu tekrar gonderildi.');
    } catch (err) {
      setError(err.message);
    } finally {
      setResending(false);
    }
  };

  return (
    <Layout>
      <div className="form-wrap">
        <h2>Giris Yap</h2>
        {notice && <div className="info" role="status" aria-live="polite">{notice}</div>}
        {error && <div className="error">{error}</div>}
        <form onSubmit={submit} className="form">
          <label>E-posta<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
          <PasswordField value={password} onChange={(e) => setPassword(e.target.value)} />
          <button className="btn" type="submit">Giris Yap</button>
        </form>
        <p>
          <Link to="/forgot-password">Sifremi unuttum</Link>
        </p>
        {attempted && !networkError && (
          <p>
            <button type="button" className="btn" onClick={resend} disabled={resending}>
              {resending ? 'Gonderiliyor...' : 'Dogrulama kodunu tekrar gonder'}
            </button>
          </p>
        )}
        <p>
          <button type="button" className="btn" onClick={() => navigate('/register')}>Hesabınız yoksa kaydolun</button>
        </p>
      </div>
    </Layout>
  );
}