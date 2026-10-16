import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { IconEye, IconEyeOff } from '../components/icons';
import { useAuth } from '../store/AuthContext';

export function PasswordField({ value, onChange, label }) {
  const [show, setShow] = useState(false);
  return (
    <label>{label || 'Şifre'}
      <span className="pw-wrap">
        <input type={show ? 'text' : 'password'} value={value} onChange={onChange} required />
        <button type="button" className="pw-toggle" onClick={() => setShow((s) => !s)} title={show ? 'Gizle' : 'Göster'}>
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
  const { login } = useAuth();
  const navigate = useNavigate();

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      const user = await login(email, password);
      navigate(user.role === 'admin' ? '/admin/products' : '/');
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <Layout>
      <div className="form-wrap">
        <h2>Giriş Yap</h2>
        {error && <div className="error">{error}</div>}
        <form onSubmit={submit} className="form">
          <label>E-posta<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
          <PasswordField value={password} onChange={(e) => setPassword(e.target.value)} />
          <button className="btn" type="submit">Giriş Yap</button>
        </form>
        <p>Hesabın yok mu? <Link to="/register">Kayıt Ol</Link></p>
      </div>
    </Layout>
  );
}
