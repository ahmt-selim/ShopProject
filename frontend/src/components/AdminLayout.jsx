import { NavLink, Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';

const items = [
  { to: '/admin/products', label: 'Ürünler' },
  { to: '/admin/orders', label: 'Siparişler' },
  { to: '/admin/categories', label: 'Kategoriler' },
  { to: '/admin/brands', label: 'Markalar' },
  { to: '/admin/customers', label: 'Müşteriler' },
  { to: '/admin/settings', label: 'Ayarlar' },
];

export default function AdminLayout() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  if (!user) return <div className="admin-guard">Yönetim paneli için giriş yapın. <Link to="/login">Giriş Yap</Link></div>;
  if (!isAdmin) return <div className="admin-guard">Bu sayfa sadece admin için.</div>;

  return (
    <div className="admin-shell">
      <aside className="admin-side">
        <Link to="/" className="admin-logo">MiniShop Panel</Link>
        <nav>
          {items.map((i) => (
            <NavLink key={i.to} to={i.to} className={({ isActive }) => (isActive ? 'active' : '')}>
              {i.label}
            </NavLink>
          ))}
        </nav>
        <div className="admin-side-foot">
          <Link to="/">Siteye Dön</Link>
          <button onClick={() => { logout(); navigate('/'); }}>Çıkış</button>
        </div>
      </aside>
      <main className="admin-content">
        <Outlet />
      </main>
    </div>
  );
}
