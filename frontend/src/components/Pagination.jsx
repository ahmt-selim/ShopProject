export default function Pagination({ page, totalPages, onChange }) {
  if (!totalPages || totalPages <= 1) return null;
  const start = Math.max(1, Math.min(page - 2, totalPages - 4));
  const nums = Array.from({ length: Math.min(5, totalPages) }, (_, i) => start + i);
  return (
    <div className="pagination">
      <button className="hbtn pagination-nav" disabled={page <= 1} onClick={() => onChange(page - 1)} aria-label="Önceki sayfa">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <polyline points="15 18 9 12 15 6" />
        </svg>
      </button>
      {nums.map((n) => (
        <button key={n} className={`hbtn${n === page ? ' primary' : ''}`} onClick={() => onChange(n)}>{n}</button>
      ))}
      <button className="hbtn pagination-nav" disabled={page >= totalPages} onClick={() => onChange(page + 1)} aria-label="Sonraki sayfa">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <polyline points="9 18 15 12 9 6" />
        </svg>
      </button>
    </div>
  );
}