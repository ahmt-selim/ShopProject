export default function Pagination({ page, totalPages, onChange }) {
  if (!totalPages || totalPages <= 1) return null;
  const start = Math.max(1, Math.min(page - 2, totalPages - 4));
  const nums = Array.from({ length: Math.min(5, totalPages) }, (_, i) => start + i);
  return (
    <div className="pagination">
      <button className="hbtn" disabled={page <= 1} onClick={() => onChange(page - 1)}>‹ Önceki</button>
      {nums.map((n) => (
        <button key={n} className={`hbtn${n === page ? ' primary' : ''}`} onClick={() => onChange(n)}>{n}</button>
      ))}
      <button className="hbtn" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>Sonraki ›</button>
    </div>
  );
}
