import { useEffect, useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { PasswordField } from './Login';
import { useAuth } from '../store/AuthContext';
import { api } from '../api/client';

const MIN_LENGTH = 8;

export default function ForgotPassword() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState('email'); // email | code | password | done
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (step !== 'done') return;
    const id = setTimeout(() => navigate('/login'), 1000);
    return () => clearTimeout(id);
  }, [step, navigate]);

  if (user) return <Navigate to="/" replace />;

  const sendCode = async (e) => {
    e?.preventDefault();
    if (loading) return;
    setError('');
    setNotice('');
    const value = email.trim();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
      setError('Geçerli bir e-posta adresi girin.');
      return;
    }
    setLoading(true);
    try {
      await api.forgotPassword(value);
      setStep('code');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const verifyCode = async (e) => {
    e.preventDefault();
    if (loading) return;
    setError('');
    setNotice('');
    const cleanCode = code.trim();
    if (!/^\d{6}$/.test(cleanCode)) {
      setError('6 haneli doğrulama kodunu girin.');
      return;
    }
    setLoading(true);
    try {
      await api.verifyResetCode({ email: email.trim(), code: cleanCode });
      setStep('password');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const submitReset = async (e) => {
    e.preventDefault();
    if (loading) return;
    setError('');
    setNotice('');
    if (password.length < MIN_LENGTH) {
      setError(`Şifre en az ${MIN_LENGTH} karakter olmalı.`);
      return;
    }
    if (password !== confirm) {
      setError('Şifreler eşleşmiyor.');
      return;
    }
    setLoading(true);
    try {
      await api.resetPassword({ email: email.trim(), code: code.trim(), newPassword: password });
      setStep('done');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <div className="form-wrap">
        <h2>Şifremi Unuttum</h2>
        {step === 'done' ? (
          <div className="success" role="status" aria-live="polite">
            Şifren yenilendi. Şimdi Giriş ekranına yönlendiriliyorsunuz…
          </div>
        ) : step === 'password' ? (
          <>
            {error && (
              <div className="error" role="alert" aria-live="assertive">
                {error}
              </div>
            )}
            <form onSubmit={submitReset} className="form">
              <PasswordField
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                label={`Yeni şifre (min ${MIN_LENGTH})`}
              />
              <PasswordField
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
                label="Yeni şifre (tekrar)"
              />
              <button className="btn" type="submit" disabled={loading}>
                {loading ? 'Kaydediliyor…' : 'Şifremi Güncelle'}
              </button>
            </form>
          </>
        ) : step === 'code' ? (
          <>
            <div className="info" role="status" aria-live="polite">
              Bu e-posta ile kayıtlı bir hesap varsa, 6 haneli doğrulama kodu gönderildi.
            </div>
            {notice && (
              <div className="info" role="status" aria-live="polite">
                {notice}
              </div>
            )}
            {error && (
              <div className="error" role="alert" aria-live="assertive">
                {error}
                <p>
                  <button type="button" className="btn" onClick={sendCode} disabled={loading}>
                    Yeni kod iste
                  </button>
                </p>
              </div>
            )}
            <form onSubmit={verifyCode} className="form">
              <label>
                Doğrulama kodu
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
                {loading ? 'Doğrulanıyor…' : 'Kodu Doğrula'}
              </button>
            </form>
            <p>
              <Link to="/login">Giriş sayfasına dön</Link>
            </p>
          </>
        ) : (
          <>
            {error && (
              <div className="error" role="alert" aria-live="assertive">
                {error}
              </div>
            )}
            <form onSubmit={sendCode} className="form">
              <label>
                E-posta
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                />
              </label>
              <button className="btn" type="submit" disabled={loading}>
                {loading ? 'Gönderiliyor…' : 'Doğrulama Kodu Gönder'}
              </button>
            </form>
            <p>
              <Link to="/login">Giriş sayfasına dön</Link>
            </p>
          </>
        )}
      </div>
    </Layout>
  );
}
