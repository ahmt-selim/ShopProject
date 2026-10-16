import { useMemo, useRef, useState } from 'react';
import { api } from '../../api/client';

const MAX_IMAGES = 5;
const MAX_BYTES = 5 * 1024 * 1024;
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const reasonText = {
  not_purchased: 'Bu ürünü henüz satın almadınız. Yorum yazmak için ürünü alıp teslim almanız gerekir.',
  order_not_completed: 'Siparişiniz teslim edildikten sonra yorum yazabilirsiniz.',
  already_reviewed: 'Bu ürüne zaten yorum yazdınız. Yorumunuzu listeden düzenleyebilirsiniz.',
};

// eligibility.canReview true ise form, değilse açıklayıcı mesaj.
// Düzenleme modunda initial + onCancel verilir.
export default function ReviewForm({ productId, eligibility, user, initial, onDone, onCancel }) {
  const [rating, setRating] = useState(initial?.rating ?? 0);
  const [hover, setHover] = useState(0);
  const [title, setTitle] = useState(initial?.title ?? '');
  const [comment, setComment] = useState(initial?.comment ?? '');
  const [keptIds, setKeptIds] = useState(() => (initial?.images || []).map((i) => i.id));
  const [files, setFiles] = useState([]);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const fileRef = useRef(null);
  const previews = useMemo(() => files.map((f) => ({ file: f, url: URL.createObjectURL(f) })), [files]);

  // Düzenleme modunda (initial) yetki zaten listede belli; gating yok.
  // Yazma modu sadece sipariş detayından açılır (OrderReviewBox), ürün sayfasında form yok.
  if (!initial) {
    if (!user) {
      return (
        <div className="info">
          Yorum yazmak için <a className="link" style={{ color: 'var(--primary)' }} href="/login">giriş yapın</a>.
        </div>
      );
    }
    if (!eligibility) return <div className="rev-skeleton">Kontrol ediliyor…</div>;
    if (!eligibility.canReview) {
      return <div className="info">{reasonText[eligibility.reason] || 'Şu anda yorum yazamazsınız.'}</div>;
    }
  }

  const keptCount = keptIds.length;
  const addFiles = (list) => {
    setError('');
    const picked = [...list];
    if (keptCount + files.length + picked.length > MAX_IMAGES) {
      setError(`En fazla ${MAX_IMAGES} resim ekleyebilirsiniz.`);
      return;
    }
    for (const f of picked) {
      if (!ALLOWED_TYPES.includes(f.type)) {
        setError(`"${f.name}": sadece jpg, png, webp yüklenebilir.`);
        return;
      }
      if (f.size > MAX_BYTES) {
        setError(`"${f.name}": her resim en fazla 5MB olmalı.`);
        return;
      }
    }
    setFiles((prev) => [...prev, ...picked]);
  };

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    if (rating < 1 || rating > 5) {
      setError('Lütfen 1-5 arası puan seçin.');
      return;
    }
    if (title.trim().length > 100) {
      setError('Başlık en fazla 100 karakter olmalı.');
      return;
    }
    if (comment.trim().length > 2000) {
      setError('Yorum en fazla 2000 karakter olmalı.');
      return;
    }
    setSaving(true);
    try {
      if (initial) {
        await api.updateReview(initial.id, {
          rating, title, comment, keepImageIds: keptIds, images: files,
        });
      } else {
        await api.createReview(productId, { rating, title, comment, images: files });
      }
      onDone();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const shown = hover || rating;

  return (
    <form className="form rev-form" onSubmit={submit}>
      <h4>{initial ? 'Yorumunu Düzenle' : 'Yorum Yaz'}</h4>
      <div role="radiogroup" aria-label="Puan seçin" className="star-input">
        {[1, 2, 3, 4, 5].map((n) => (
          <button
            key={n}
            type="button"
            role="radio"
            aria-checked={rating === n}
            aria-label={`${n} yıldız`}
            className={n <= shown ? 'on' : ''}
            onClick={() => setRating(n)}
            onMouseEnter={() => setHover(n)}
            onMouseLeave={() => setHover(0)}
            onFocus={() => setHover(n)}
            onBlur={() => setHover(0)}
          >★</button>
        ))}
        <span className="muted">{rating > 0 ? `${rating} / 5` : 'Puan seçin'}</span>
      </div>
      <label>
        Başlık (opsiyonel, en fazla 100 karakter)
        <input value={title} maxLength={110} onChange={(e) => setTitle(e.target.value)} placeholder="Özet başlık" />
      </label>
      <label>
        Yorumunuz (opsiyonel, en fazla 2000 karakter)
        <textarea value={comment} maxLength={2100} onChange={(e) => setComment(e.target.value)} placeholder="Ürün hakkındaki deneyiminiz…" />
        <span className="muted">{comment.trim().length} / 2000</span>
      </label>
      <div>
        <div className="muted">Resimler ({keptCount + files.length} / {MAX_IMAGES})</div>
        <div className="rev-thumbs">
          {(initial?.images || []).filter((img) => keptIds.includes(img.id)).map((img) => (
            <span key={img.id} className="rev-thumb">
              <img src={api.imageUrl(img.url)} alt="" />
              <button type="button" className="rev-thumb-x" aria-label="Resmi kaldır"
                onClick={() => setKeptIds((prev) => prev.filter((id) => id !== img.id))}>×</button>
            </span>
          ))}
          {previews.map((p, i) => (
            <span key={`${p.file.name}-${i}`} className="rev-thumb">
              <img src={p.url} alt="" />
              <button type="button" className="rev-thumb-x" aria-label={`${p.file.name} dosyasını kaldır`}
                onClick={() => setFiles((prev) => prev.filter((_, j) => j !== i))}>×</button>
            </span>
          ))}
        </div>
        {keptCount + files.length < MAX_IMAGES && (
          <div className="file-picker" style={{ marginTop: 8 }}>
            <input
              ref={fileRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              multiple
              hidden
              onChange={(e) => { addFiles(e.target.files); e.target.value = ''; }}
            />
            <button type="button" className="hbtn" onClick={() => fileRef.current?.click()}>
              Resim Ekle
            </button>
          </div>
        )}
      </div>
      {error && <div className="error">{error}</div>}
      <div className="row">
        <button className="btn" disabled={saving}>{saving ? 'Kaydediliyor…' : initial ? 'Güncelle' : 'Yorumu Gönder'}</button>
        {initial && <button type="button" className="hbtn" disabled={saving} onClick={onCancel}>Vazgeç</button>}
      </div>
    </form>
  );
}
