// Yeniden kullanılabilir yıldız gösterimi (0.5 hassasiyet, salt-okunur).
// value: ortalama puan (0-5), reviewCount: yorum sayısı (null ise sayı gizlenir).
export default function StarRating({ value = 0, reviewCount = null, size = 16 }) {
  const safe = Math.max(0, Math.min(5, Number(value) || 0));
  const pct = (safe / 5) * 100;
  const label =
    reviewCount === null
      ? `${safe.toFixed(1)} / 5 puan`
      : reviewCount === 0
        ? 'Henüz yorum yok'
        : `${safe.toFixed(1)} / 5 (${reviewCount} yorum)`;

  return (
    <span className="stars" style={{ fontSize: size }} role="img" aria-label={label} title={label}>
      <span className="stars-wrap" aria-hidden="true">
        <span className="stars-bg">★★★★★</span>
        <span className="stars-fg" style={{ width: `${pct}%` }}>★★★★★</span>
      </span>
      {reviewCount !== null && reviewCount > 0 && (
        <span className="stars-count">({reviewCount})</span>
      )}
      {reviewCount === 0 && <span className="stars-empty">Henüz yorum yok</span>}
    </span>
  );
}
