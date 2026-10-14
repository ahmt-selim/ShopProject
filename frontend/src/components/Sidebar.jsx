import { useNavigate } from 'react-router-dom';
import { useAuth } from '../store/AuthContext';

export default function Sidebar({ categories, selected, onSelect }) {
  const { isAdmin } = useAuth();
  const navigate = useNavigate();

  return (
    <aside className="sidebar">
      <h3>Kategoriler</h3>
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
      {isAdmin && (
        <button className="btn admin-go" onClick={() => navigate('/admin/products')}>
          Yönetim Paneli
        </button>
      )}
    </aside>
  );
}
