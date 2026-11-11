/* eslint-disable react/prop-types -- proje genelinde propTypes kullanılmıyor (düz JSX) */
import { useState } from 'react';
import { useSidebar } from '../store/SidebarContext';

export default function Sidebar({ categories, selected, onSelect, filters = {}, onFilterChange }) {
  const { open, toggle } = useSidebar();
  const [catQuery, setCatQuery] = useState('');
  const closeIfMobile = () => {
    if (window.matchMedia('(max-width: 800px)').matches) toggle();
  };
  const filteredCategories = (categories || []).filter((c) =>
    (c.name || '').toLowerCase().includes(catQuery.toLowerCase())
  );

  return (
    <>
      {open && <div className="sidebar-backdrop" onClick={toggle} aria-hidden="true" />}
      <aside className={`sidebar ${open ? 'open' : 'closed'}`} aria-label="Kategori kenar çubuğu">
        {open && <h3 className="sidebar-title">Kategoriler</h3>}
        {open && (
          <input
            className="sidebar-cat-search"
            type="search"
            placeholder="Kategori ara..."
            value={catQuery}
            onChange={(e) => setCatQuery(e.target.value)}
            aria-label="Kategori ara"
          />
        )}

        {open && (
          <nav id="sidebar-categories" className="sidebar-categories">
            <ul>
              <li>
                <button
                  className={!selected ? 'active' : ''}
                  onClick={() => { onSelect(null); closeIfMobile(); }}
                >
                  Tüm Kategoriler
                </button>
              </li>
              {filteredCategories.map((c) => (
                <li key={c.id}>
                  <button
                    className={selected === c.id ? 'active' : ''}
                    onClick={() => { onSelect(c.id); closeIfMobile(); }}
                  >
                    {c.name} ({c.productCount})
                  </button>
                </li>
              ))}
            </ul>
          </nav>
        )}
        {open && (
          <div className="sidebar-filters">
            <h3>Filtreler</h3>
            <label className="filter-checkbox">
              <input
                type="checkbox"
                checked={!!filters.discounted}
                onChange={() => { onFilterChange?.('discounted'); closeIfMobile(); }}
              />
              İndirimli Ürünler
            </label>
            <label className="filter-checkbox">
              <input
                type="checkbox"
                checked={!!filters.freeShipping}
                onChange={() => { onFilterChange?.('freeShipping'); closeIfMobile(); }}
              />
              Ücretsiz Kargo
            </label>
          </div>
        )}
      </aside>
    </>
  );
}