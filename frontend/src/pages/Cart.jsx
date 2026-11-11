import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { api } from '../api/client';
import { useCart } from '../store/CartContext';
import { useAuth } from '../store/AuthContext';

export default function Cart() {
  const { items, setQty, remove, clear, totalPrice } = useCart();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [customTexts, setCustomTexts] = useState({});
  const [shippingFee, setShippingFee] = useState(49.99);
  const [freeShippingLimit, setFreeShippingLimit] = useState(0);

  useEffect(() => {
    api.publicSettings()
      .then((s) => {
        setShippingFee(Number(s?.shippingFee || 49.99));
        setFreeShippingLimit(Number(s?.freeShippingLimit || 0));
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    setCustomTexts((prev) => {
      const next = { ...prev };
      items.forEach((i) => {
        const len = i.isCustomizable ? i.qty : 0;
        if (len > 0) {
          const cur = Array.isArray(next[i.id]) ? next[i.id] : [];
          if (cur.length !== len) {
            const arr = Array.from({ length: len }, (_, idx) => cur[idx] || '');
            next[i.id] = arr;
          }
        } else {
          delete next[i.id];
        }
      });
      return next;
    });
  }, [items]);

  const updateText = (itemId, idx, value) => {
    setCustomTexts((prev) => {
      const arr = [...(prev[itemId] || [])];
      arr[idx] = value;
      return { ...prev, [itemId]: arr };
    });
  };

  const checkout = async () => {
    // doğrulama
    for (const i of items) {
      if (!i.isCustomizable) continue;
      const arr = customTexts[i.id] || [];
      if (arr.length !== i.qty) {
        alert(`"${i.name}" için ${i.qty} adet metin girilmeli.`);
        return;
      }
      for (const t of arr) {
        if (!t || !t.trim()) {
          alert(`"${i.name}" için boş metin var.`);
          return;
        }
        const max = i.maxCustomTextLength || 0;
        if (max > 0 && t.trim().length > max) {
          alert(`"${i.name}" metni en fazla ${max} karakter olabilir.`);
          return;
        }
      }
    }

    if (!user) {
      navigate('/login');
      return;
    }

    // customTexts'i productId -> list map'e çevir
    const map = {};
    items.forEach((i) => {
      if (i.isCustomizable) {
        map[i.productId || i.id] = (customTexts[i.id] || []).map((t) => t.trim());
      }
    });

    // adres seçimi checkout sayfasında; burada direkt payment initialize? Mevcut akış /checkout'a gidiyor.
    // Sepette metinleri geçici olarak sakla.
    try {
      sessionStorage.setItem('cartCustomTexts', JSON.stringify(map));
    } catch {}
    navigate('/checkout');
  };

  return (
    <Layout>
      <h2>Sepet</h2>
      {items.length === 0 ? (
        <p>Sepetin boş. Anasayfadan ürün ekle.</p>
      ) : (
        <>
          <div className="cart-list">
            {items.map((i) => (
              <div key={i.id} className="cart-item">
                <img src={api.imageUrl(i.imageUrl)} alt={i.name} />
                <div className="cart-info">
                  <strong>{i.name}</strong>
                  <div className="muted">{Number(i.price).toLocaleString('tr-TR')} ₺</div>
                  {i.isCustomizable && (
                    <div style={{ marginTop: 8 }}>
                      <div className="muted" style={{ marginBottom: 4 }}>
                        Kişiye özel metin (maks {i.maxCustomTextLength || '-'} karakter)
                      </div>
                      {(customTexts[i.id] || []).map((t, idx) => (
                        <input
                          key={idx}
                          value={t}
                          maxLength={i.maxCustomTextLength > 0 ? i.maxCustomTextLength : undefined}
                          onChange={(e) => updateText(i.id, idx, e.target.value)}
                          placeholder={`${idx + 1}. metin`}
                          style={{ display: 'block', marginBottom: 4, width: Math.max(120, Math.min((i.maxCustomTextLength || 20) * 8, 360)) }}
                        />
                      ))}
                    </div>
                  )}
                  <div className="qty">
                    <button onClick={() => setQty(i.productId || i.id, i.qty - 1)}>-</button>
                    <span>{i.qty}</span>
                    <button onClick={() => setQty(i.productId || i.id, i.qty + 1)}>+</button>
                  </div>
                </div>
                <button className="link" onClick={() => remove(i.productId || i.id)}>Kaldır</button>
              </div>
            ))}
          </div>
          <h3 style={{ marginBottom: 8 }}>Toplam: {totalPrice.toLocaleString('tr-TR')} ₺</h3>
          {(() => {
            const free = freeShippingLimit > 0 && totalPrice >= freeShippingLimit;
            const shippingCost = free ? 0 : shippingFee;
            const grandTotal = totalPrice + shippingCost;
            return (
              <>
                <div className="muted" style={{ marginTop: 4, marginBottom: 8 }}>
                  Kargo Bedeli:{' '}
                  {free ? (
                    <>
                      <s>{shippingFee.toLocaleString('tr-TR')} ₺</s> <span>(Ücretsiz)</span>
                    </>
                  ) : (
                    `${shippingFee.toLocaleString('tr-TR')} ₺`
                  )}
                </div>
                <h3 style={{ marginTop: 8, marginBottom: 16 }}>
                  Genel Toplam: {grandTotal.toLocaleString('tr-TR')} ₺
                </h3>
              </>
            );
          })()}
          <div className="row" style={{ marginTop: 8 }}>
            <button className="btn" onClick={checkout}>Siparişi Tamamla</button>
            <button className="hbtn" onClick={clear}>Sepeti Temizle</button>
          </div>
        </>
      )}
    </Layout>
  );
}