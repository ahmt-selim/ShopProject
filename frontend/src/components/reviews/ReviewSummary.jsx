import StarRating from '../StarRating';

// Büyük ortalama + 5'ten 1'e dağılım çubukları. Çubuğa tıklayınca o puana filtreler.
export default function ReviewSummary({ summary, activeRating, onSelectRating }) {
  if (!summary) return null;
  const { averageRating, reviewCount, distribution } = summary;

  return (
    <div className="rev-summary">
      <div className="rev-summary-left">
        <div className="rev-avg">{Number(averageRating).toFixed(1).replace('.', ',')}</div>
        <StarRating value={averageRating} reviewCount={null} size={18} />
        <div className="muted">{reviewCount} yorum</div>
      </div>
      <div className="rev-bars" role="group" aria-label="Puana göre filtrele">
        {[5, 4, 3, 2, 1].map((star) => {
          const count = distribution?.[String(star)] ?? 0;
          const pct = reviewCount > 0 ? Math.round((count / reviewCount) * 100) : 0;
          const active = activeRating === star;
          return (
            <button
              key={star}
              className={`rev-bar${active ? ' active' : ''}`}
              onClick={() => onSelectRating(active ? null : star)}
              aria-pressed={active}
              title={active ? 'Filtreyi temizle' : `${star} yıldızlı yorumları göster`}
            >
              <span className="rev-bar-star">{star} ★</span>
              <span className="rev-bar-track"><span className="rev-bar-fill" style={{ width: `${pct}%` }} /></span>
              <span className="rev-bar-count">({count})</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
