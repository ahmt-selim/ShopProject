import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCart } from '../store/CartContext';
import { useAuth } from '../store/AuthContext';
import { useTheme } from '../themes/ThemeContext';
import { IconCart, IconHeart, IconUser, IconTheme } from './icons';

export default function Header() {
  const { totalCount } = useCart();
  const { user, logout } = useAuth();
  const { themeId, setThemeId, themes } = useTheme();
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  const navigate = useNavigate();

  const ids = Object.keys(themes);
  const cycleTheme = () => setThemeId(ids[(ids.indexOf(themeId) + 1) % ids.length]);

  useEffect(() => {
    const close = (e) => {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
    };
    const esc = (e) => {
      if (e.key === 'Escape') setMenuOpen(false);
    };
    document.addEventListener('mousedown', close);
    document.addEventListener('keydown', esc);
    return () => {
      document.removeEventListener('mousedown', close);
      document.removeEventListener('keydown', esc);
    };
  }, []);

  const go = (to) => {
    setMenuOpen(false);
    navigate(to);
  };

  const onLogout = () => {
    setMenuOpen(false);
    logout();
    navigate('/');
  };

  return (
    <header className="header">
      <Link to="/" className="logo" title="ihtiyacın3D">
        <img src="/logo-header.png" alt="ihtiyacın3D" className="logo-img" />
      </Link>
      <div className="header-right">
        <button className="icon-btn" onClick={cycleTheme} title={`Tema: ${themes[themeId]?.name || themeId}`}>
          <IconTheme />
        </button>
        <Link to="/favorites" className="icon-btn" title="Favoriler">
          <IconHeart filled={false} />
        </Link>
        <Link to="/cart" className="icon-btn cart-btn" title="Sepet">
          <IconCart />
          {totalCount > 0 && <span className="cart-badge">{totalCount}</span>}
        </Link>
        {!user ? (
          <>
            <Link to="/register" className="hbtn">Kayıt Ol</Link>
            <Link to="/login" className="hbtn primary">Giriş Yap</Link>
          </>
        ) : (
          <div className="menu-wrap" ref={menuRef}>
            <button className="icon-btn" onClick={() => setMenuOpen((o) => !o)} title="Hesabım">
              <IconUser />
            </button>
            {menuOpen && (
              <div className="dropdown">
                <button onClick={() => go('/account/profile')}>Hesap Bilgileri</button>
                <button onClick={() => go('/account/addresses')}>Adreslerim</button>
                <button onClick={() => go('/account/orders')}>Siparişlerim</button>
                <button onClick={() => go('/account/support')}>Destek</button>
                <button className="danger" onClick={onLogout}>Çıkış Yap</button>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
}
