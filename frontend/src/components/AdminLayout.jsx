import { NavLink, Outlet, Link } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';
import { SidebarProvider, useSidebar } from '../store/SidebarContext';
import Header from './Header';

const items = [
  { to: '/admin/products', label: 'Ürünler' },
  { to: '/admin/orders', label: 'Siparişler' },
  { to: '/admin/categories', label: 'Kategoriler' },
  { to: '/admin/brands', label: 'Markalar' },
  { to: '/admin/customers', label: 'Müşteriler' },
  { to: '/admin/settings', label: 'Ayarlar' },
];

function AdminShell() {
  const { open, toggle } = useSidebar();
  return (
    <div className="admin-page">
      <Header />
      {open && <div className="admin-backdrop" onClick={toggle} aria-hidden="true" />}
      <div className={`admin-shell ${open ? 'side-open' : 'side-closed'}`}>
        {open && (
          <aside className="admin-side">
            <nav>
              {items.map((i) => (
                <NavLink key={i.to} to={i.to} className={({ isActive }) => (isActive ? 'active' : '')} onClick={() => { if (window.matchMedia('(max-width: 800px)').matches) toggle(); }}>
                  {i.label}
                </NavLink>
              ))}
            </nav>
          </aside>
        )}
        <main className="admin-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default function AdminLayout() {
  const { user, isAdmin } = useAuth();

  if (!user) return <div className="admin-guard">Yönetim paneli için giriş yapın. <Link to="/login">Giriş Yap</Link></div>;
  if (!isAdmin) return <div className="admin-guard">Bu sayfa sadece admin için.</div>;

  return (
    <SidebarProvider>
      <AdminShell />
    </SidebarProvider>
  );
}