import { useState } from 'react';
import { api } from '../../api/client';
import StarRating from '../StarRating';
import ReviewGallery from './ReviewGallery';

// Tek yorum kartı: maskeli ad, tarih, yıldız, başlık, metin (düz metin, escape React'te),
// doğrulanmış rozeti, resim küçükleri + lightbox, kendi yorumuysa düzenle/sil.
export default function ReviewItem({ review, onEdit, onDeleted }) {
  const [lightbox, setLightbox] = useState(null);
  const [confirming, setConfirming] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState('');

  const date = new Date(review.createdAt).toLocaleDateString('tr-TR', {
    day: 'numeric', month: 'long', year: 'numeric',
  });

  const remove = async () => {
    setDeleting(true);
    setError('');
    try {
      await api.deleteReview(review.id);
      onDeleted(review.id);
    } catch (err) {
      setError(err.message);
    } finally {
      setDeleting(false);
      setConfirming(false);
    }
  };

  return (
    <article className="rev-item">
      <div className="rev-head">
        <div>
          <strong>{review.displayName}</strong>
          <div className="muted rev-date">{date}{review.updatedAt !== review.createdAt ? ' (düzenlendi)' : ''}</div>
        </div>
        <StarRating value={review.rating} reviewCount={null} size={15} />
      </div>
      {review.title && <div className="rev-title">{review.title}</div>}
      {review.comment && <p className="rev-text">{review.comment}</p>}
      {review.images?.length > 0 && (
        <div className="rev-thumbs">
          {review.images.map((img, i) => (
            <button key={img.id} className="rev-thumb" onClick={() => setLightbox(i)}
              aria-label={`${i + 1}. yorumu resmini büyüt`}>
              <img src={api.imageUrl(img.url)} alt="" loading="lazy" />
            </button>
          ))}
        </div>
      )}
      {review.isMine && (
        <div className="row rev-actions">
          <button className="hbtn" onClick={() => onEdit(review)}>Düzenle</button>
          {!confirming ? (
            <button className="hbtn" onClick={() => setConfirming(true)}>Sil</button>
          ) : (
            <span className="confirm-box" role="alertdialog" aria-label="Silme onayı">
              Emin misiniz?
              <button className="hbtn" disabled={deleting} onClick={remove}>
                {deleting ? 'Siliniyor…' : 'Evet, sil'}
              </button>
              <button className="hbtn" disabled={deleting} onClick={() => setConfirming(false)}>Vazgeç</button>
            </span>
          )}
        </div>
      )}
      {error && <div className="error">{error}</div>}
      {lightbox !== null && (
        <ReviewGallery
          images={review.images}
          index={lightbox}
          onClose={() => setLightbox(null)}
          onNav={(d) => setLightbox((i) => (i + d + review.images.length) % review.images.length)}
        />
      )}
    </article>
  );
}
