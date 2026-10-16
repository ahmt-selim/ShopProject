import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';
import { useAuth } from '../../store/AuthContext';
import ReviewSummary from './ReviewSummary';
import ReviewItem from './ReviewItem';
import ReviewForm from './ReviewForm';

const PAGE_SIZE = 5;

// Ürün detay sayfasındaki salt-okunur "Müşteri Yorumları" bölümü: özet,
// filtre/sıralama, liste. Yazma formu burada YOK (sadece sipariş detayında);
// kendi yorumunu düzenleme formu açık olabilir.
export default function ReviewsSection({ productId, onSummaryChange }) {
  const { user } = useAuth();
  const [summary, setSummary] = useState(null);
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [sort, setSort] = useState('newest');
  const [rating, setRating] = useState(null);
  const [withImages, setWithImages] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState('');
  const [editing, setEditing] = useState(null);

  const loadSummary = useCallback(async () => {
    try {
      const s = await api.getReviewSummary(productId);
      setSummary(s);
      onSummaryChange?.(s);
    } catch { /* liste hatası aşağıda gösterilir */ }
  }, [productId, onSummaryChange]);

  const loadPage = useCallback(async (p, append) => {
    if (append) setLoadingMore(true);
    else setLoading(true);
    setError('');
    try {
      const res = await api.getReviews(productId, {
        page: p, pageSize: PAGE_SIZE,
        sort, rating: rating || undefined, withImages: withImages || undefined,
      });
      setItems((prev) => (append ? [...prev, ...res.items] : res.items));
      setTotal(res.total);
      setPage(res.page);
      setTotalPages(res.totalPages);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  }, [productId, sort, rating, withImages]);

  useEffect(() => {
    loadSummary();
  }, [loadSummary]);

  useEffect(() => {
    setPage(1);
    loadPage(1, false);
  }, [loadPage]);

  const refresh = useCallback(() => {
    setEditing(null);
    loadSummary();
    loadPage(1, false);
  }, [loadSummary, loadPage]);

  return (
    <section className="reviews" id="reviews" aria-label="Müşteri yorumları">
      <h3>Müşteri Yorumları</h3>

      {summary && summary.reviewCount > 0 && (
        <ReviewSummary
          summary={summary}
          activeRating={rating}
          onSelectRating={(r) => setRating(r)}
        />
      )}

      <div className="rev-filters">
        <label>
          Sıralama
          <select value={sort} onChange={(e) => setSort(e.target.value)} aria-label="Yorum sıralaması">
            <option value="newest">En yeni</option>
            <option value="highest">En yüksek puan</option>
            <option value="lowest">En düşük puan</option>
          </select>
        </label>
        <label className="rev-check">
          <input type="checkbox" checked={withImages} onChange={(e) => setWithImages(e.target.checked)} />
          Sadece resimli yorumlar
        </label>
        {rating && (
          <button className="hbtn" onClick={() => setRating(null)}>
            {rating} ★ filtresi ×
          </button>
        )}
      </div>

      {loading ? (
        <div aria-label="Yorumlar yükleniyor">
          {[0, 1].map((i) => <div key={i} className="rev-skeleton" />)}
        </div>
      ) : error ? (
        <div className="error">
          {error} <button className="hbtn" onClick={() => loadPage(1, false)}>Tekrar Dene</button>
        </div>
      ) : items.length === 0 ? (
        <p className="muted">Henüz yorum yok. Bu ürünü alıp ilk yorumu siz yazın.</p>
      ) : (
        <>
          <div className="rev-list">
            {items.map((r) => (
              <ReviewItem key={r.id} review={r} onEdit={setEditing} onDeleted={refresh} />
            ))}
          </div>
          {page < totalPages && (
            <button className="hbtn" disabled={loadingMore} onClick={() => loadPage(page + 1, true)}>
              {loadingMore ? 'Yükleniyor…' : `Daha Fazla Göster (${total - items.length} kaldı)`}
            </button>
          )}
        </>
      )}

      <div className="rev-form-wrap">
        {editing && (
          <ReviewForm
            productId={productId}
            user={user}
            initial={editing}
            onDone={refresh}
            onCancel={() => setEditing(null)}
          />
        )}
      </div>
    </section>
  );
}
