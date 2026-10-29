/* eslint-disable react/prop-types -- proje genelinde propTypes kullanılmıyor (düz JSX) */
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { siteInfo } from '../config/siteInfo';
import { APP_VERSION_LABEL } from '../config/version';
import { IconInstagram, IconWhatsApp } from './icons';
import './Footer.css';

const corporateLinks = [
  { to: '/info/hakkimizda', label: 'Hakkımızda' },
  { to: '/info/iletisim', label: 'Bize Ulaşın' },
  { to: '/info/blog', label: 'Blog' },
  { to: '/info/kvkk', label: 'KVKK Aydınlatma Metni' },
];

const helpLinks = [
  { to: '/siparis-takip', label: 'Sipariş Takibi' },
  { to: '/info/teslimat', label: 'Teslimat Bilgileri' },
  { to: '/info/iade-iptal', label: 'İade ve İptal' },
  { to: '/info/odeme', label: 'Ödeme Bilgileri' },
];

const legalLinks = [
  { to: '/info/cerez-politikasi', label: 'Çerez Politikası' },
  { to: '/info/mesafeli-satis', label: 'Mesafeli Satış' },
  { to: '/info/kullanim-kosullari', label: 'Kullanım Koşulları' },
  { to: '/info/on-bilgilendirme', label: 'Ön Bilgilendirme' },
];

function Newsletter() {
  const [email, setEmail] = useState('');
  const [status, setStatus] = useState(null);

  const submit = (e) => {
    e.preventDefault();
    const value = email.trim();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
      setStatus({ ok: false, text: 'Geçerli bir e-posta adresi girin.' });
      return;
    }
    try {
      const key = 'newsletter-emails';
      const list = JSON.parse(localStorage.getItem(key) || '[]');
      if (!list.includes(value)) {
        list.push(value);
        localStorage.setItem(key, JSON.stringify(list));
      }
    } catch {
      // localStorage kullanılamazsa da kaydı onaylanmış say.
    }
    setEmail('');
    setStatus({ ok: true, text: 'Kaydınız alındı, teşekkürler.' });
  };

  return (
    <form className="footer-newsletter" onSubmit={submit}>
      <label className="footer-newsletter-label" htmlFor="footer-newsletter-email">
        E-posta adresin
      </label>
      <div className="footer-newsletter-row">
        <input
          id="footer-newsletter-email"
          type="email"
          className="footer-newsletter-input"
          placeholder="E-posta adresin"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          autoComplete="email"
        />
        <button type="submit" className="btn footer-newsletter-btn">Kaydol</button>
      </div>
      {status && (
        <p className={`footer-newsletter-msg ${status.ok ? 'ok' : 'err'}`} role="status">
          {status.text}
        </p>
      )}
    </form>
  );
}

export default function Footer() {
  const [storeName, setStoreName] = useState(siteInfo.storeName);

  useEffect(() => {
    let cancelled = false;
    // Settings'te iletişim bilgisi yok; yalnızca site adı public endpoint'te var.
    api.publicSettings().then((s) => {
      if (!cancelled && s?.storeName) setStoreName(s.storeName);
    }).catch(() => {});
    return () => { cancelled = true; };
  }, []);

  const year = new Date().getFullYear();
  const phoneHref = `tel:${siteInfo.phone.replace(/[^+\d]/g, '')}`;

  return (
    <footer className="footer">
      <div className="footer-grid">
        <div className="footer-col">
          <h2 className="footer-title footer-brand">
            <img src="/logo-header.png" alt={storeName} className="footer-logo" />
          </h2>
          <p className="footer-text">{siteInfo.tagline}</p>
          <p className="footer-phone">
            <a href={phoneHref}>{siteInfo.phone}</a>
          </p>
          <div className="footer-social-row">
            <a
              href={siteInfo.whatsapp}
              target="_blank"
              rel="noopener noreferrer"
              className="footer-wa"
              aria-label="WhatsApp ile destek alın"
            >
              <IconWhatsApp />
            </a>
            <a
              href={siteInfo.social?.instagram || '#'}
              target="_blank"
              rel="noopener noreferrer"
              className="footer-insta"
              aria-label="Instagram'da takip edin"
            >
              <IconInstagram />
            </a>
          </div>
        </div>

        <nav aria-label="Kurumsal" className="footer-col">
          <h2 className="footer-title">Kurumsal</h2>
          <ul className="footer-links">
            {corporateLinks.map((l) => (
              <li key={l.to}><Link to={l.to}>{l.label}</Link></li>
            ))}
          </ul>
        </nav>

        {siteInfo.marketplaces?.length > 0 && (
          <nav aria-label="Diğer platformlardaki mağazalarımız" className="footer-col">
            <h2 className="footer-title">Mağazalarımız</h2>
            <ul className="footer-links">
              {siteInfo.marketplaces.map((m) => (
                <li key={m.label}>
                  <a href={m.url} target="_blank" rel="noopener noreferrer">
                    {m.label}
                  </a>
                </li>
              ))}
            </ul>
          </nav>
        )}

        <nav aria-label="Yardım" className="footer-col">
          <h2 className="footer-title">Yardım</h2>
          <ul className="footer-links">
            {helpLinks.map((l) => (
              <li key={l.to}><Link to={l.to}>{l.label}</Link></li>
            ))}
          </ul>
        </nav>

        <div className="footer-col">
          <h2 className="footer-title">Fırsatlardan haberdar olun</h2>
          <p className="footer-text">Kampanya ve yeni ürünleri e-postanıza gönderelim.</p>
          <Newsletter />
          <p className="footer-note">
            Abone olarak e-posta ile bilgilendirme almayı kabul etmiş olursunuz.
          </p>
        </div>
      </div>

      <div className="footer-bottom">
        <p>© {year} {storeName}. Tüm hakları saklıdır.</p>
        <p className="footer-version">Sürüm {APP_VERSION_LABEL}</p>
        <nav aria-label="Yasal bilgiler" className="footer-legal">
          {legalLinks.map((l, i) => (
            <span key={l.to}>
              {i > 0 && <span aria-hidden="true"> · </span>}
              <Link to={l.to}>{l.label}</Link>
            </span>
          ))}
        </nav>
        <div className="footer-pay" aria-label="Kabul edilen ödeme yöntemleri">
          <span className="pay-badge">VISA</span>
          <span className="pay-badge">Mastercard</span>
          <span className="pay-badge">Troy</span>
        </div>
      </div>
    </footer>
  );
}
