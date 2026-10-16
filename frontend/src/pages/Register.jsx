import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { PasswordField } from './Login';
import { useAuth } from '../store/AuthContext';

export default function Register() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const { register } = useAuth();
  const navigate = useNavigate();

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await register(email, password, name);
      navigate('/');
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <Layout>
      <div className="form-wrap">
        <h2>Kayıt Ol</h2>
        {error && <div className="error">{error}</div>}
        <form onSubmit={submit} className="form">
          <label>Ad Soyad<input value={name} onChange={(e) => setName(e.target.value)} placeholder="Adın" /></label>
          <label>E-posta<input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></label>
          <PasswordField value={password} onChange={(e) => setPassword(e.target.value)} label="Şifre (min 8)" />
          <button className="btn" type="submit">Kayıt Ol</button>
        </form>
        <p>Zaten üye misin? <Link to="/login">Giriş Yap</Link></p>
      </div>
    </Layout>
  );
}
