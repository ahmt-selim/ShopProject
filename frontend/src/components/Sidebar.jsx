/* eslint-disable react/prop-types -- proje genelinde propTypes kullanılmıyor (düz JSX) */
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';
import { useSidebar } from '../store/SidebarContext';

export default function Sidebar({ categories, selected, onSelect }) {
  const { isAdmin } = useAuth();
  const navigate = useNavigate();
  const { open } = useSidebar();

  return (
    <aside className={`sidebar ${open ? 'open' : 'closed'}`} aria-label="Kategori kenar çubuğu">
      {open && isAdmin && (
        <button className="btn admin-go" onClick={() => navigate('/admin/products')}>
          Yönetim Paneli
        </button>
      )}

      {open && (
        <nav id="sidebar-categories" className="sidebar-categories">
          <ul>
            <li>
              <button
                className={!selected ? 'active' : ''}
                onClick={() => onSelect(null)}
              >
                Tümü
              </button>
            </li>
            {categories.map((c) => (
              <li key={c.id}>
                <button
                  className={selected === c.id ? 'active' : ''}
                  onClick={() => onSelect(c.id)}
                >
                  {c.name} ({c.productCount})
                </button>
              </li>
            ))}
          </ul>
        </nav>
      )}
    </aside>
  );
}