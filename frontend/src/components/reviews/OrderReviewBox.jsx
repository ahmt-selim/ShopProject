import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../../api/client';
import { useAuth } from '../../store/AuthContext';
import StarRating from '../StarRating';
import ReviewForm from './ReviewForm';

// Sipariş detayındaki tek ürün satırı için yorum kutusu.
// Sadece teslim edilmiş siparişlerde gösterilir (delivered=true).
// Yazma + düzenleme + silme buradan yapılır; ürün sayfası salt-okunurdur.
export default function OrderReviewBox({ productId, productName, delivered }) {
  const { user } = useAuth();
  const [eligibility, setEligibility] = useState(null);
  const [mine, setMine] = useState(null);
  const [open, setOpen] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const reload = useCallback(async () => {
    setError('');
    try {
      const elig = await api.getReviewEligibility(productId);
      setEligibility(elig);
      if (elig.existingReviewId) {
        setMine(await api.getMyReview(productId));
      } else {
        setMine(null);
      }
    } catch (err) {
      setError(err.message);
    }
  }, [productId]);

  useEffect(() => {
    if (delivered) reload();
  }, [delivered, reload]);

  if (!delivered || !user) return null;
  if (error && !eligibility) return <div className="error">{error}</div>;

  const done = () => {
    setOpen(false);
    setConfirming(false);
    reload();
  };

  const remove = async () => {
    setBusy(true);
    try {
      await api.deleteReview(mine.id);
      done();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
      setConfirming(false);
    }
  };

  return (
    <div className="order-review">
      {mine ? (
        <>
          <div className="row">
            <StarRating value={mine.rating} reviewCount={null} size={14} />
            <span className="muted">Yorumunuz var</span>
            <Link to={`/product/${productId}`} className="link" style={{ color: 'var(--primary)' }}>
              Üründe gör
            </Link>
          </div>
          {!open && !confirming && (
            <div className="row" style={{ marginTop: 6 }}>
              <button className="hbtn" onClick={() => setOpen(true)}>Düzenle</button>
              <button className="hbtn" onClick={() => setConfirming(true)}>Sil</button>
            </div>
          )}
          {confirming && (
            <span className="confirm-box" role="alertdialog" aria-label="Silme onayı" style={{ marginTop: 6 }}>
              Yorum silinsin mi?
              <button className="hbtn" disabled={busy} onClick={remove}>
                {busy ? 'Siliniyor…' : 'Evet, sil'}
              </button>
              <button className="hbtn" disabled={busy} onClick={() => setConfirming(false)}>Vazgeç</button>
            </span>
          )}
          {open && (
            <ReviewForm
              productId={productId}
              eligibility={eligibility}
              user={user}
              initial={mine}
              onDone={done}
              onCancel={() => setOpen(false)}
            />
          )}
        </>
      ) : eligibility?.canReview ? (
        <>
          {!open ? (
            <button className="hbtn" onClick={() => setOpen(true)}>
              ★ Bu ürünü değerlendir
            </button>
          ) : (
            <ReviewForm
              productId={productId}
              eligibility={eligibility}
              user={user}
              onDone={done}
              onCancel={() => setOpen(false)}
            />
          )}
        </>
      ) : null}
      {error && <div className="error">{error}</div>}
    </div>
  );
}
