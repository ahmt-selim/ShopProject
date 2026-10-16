// Site iletişim ve sosyal medya bilgileri — tek yerden değişir.
// Gerçek iletişim bilgileri BURADA DEĞİL, git'e girmeyen `frontend/.env.local`
// dosyasında tutulur (bkz. `.env.local.example`). Aşağıdaki değerler yalnızca
// yedek placeholder'dır; `.env.local` doldurulunca onlar geçerli olur.
const env = import.meta.env || {};

export const siteInfo = {
  storeName: 'ihtiyacın3D',
  tagline: '3D baskı ile üretilen pratik ürünler — ihtiyacına özel, hızlı ve kaliteli.',
  address: env.VITE_CONTACT_ADDRESS || 'Örnek Mah. Örnek Cad. No:1, İstanbul',
  phone: env.VITE_CONTACT_PHONE || '+90 (212) 000 00 00',
  whatsapp: env.VITE_CONTACT_WHATSAPP || 'https://wa.me/905320000000',
  email: env.VITE_CONTACT_EMAIL || 'info@ornek.com',
  workingHours: env.VITE_CONTACT_HOURS || 'Hafta içi 09.00 – 18.00',
  social: {
    instagram: env.VITE_CONTACT_INSTAGRAM || '#',
    facebook: '#',
    x: '#',
    youtube: '#',
  },
  // Diğer platformlardaki mağazalarımız (footer'da "Mağazalarımız" başlığıyla çıkar).
  // Gerçek mağaza linkleriyle değiştirin; kullanılmayacak satırı silin.
  marketplaces: [
    { label: 'Trendyol', url: 'https://www.trendyol.com/magaza/ihtiyacin3d-m-603384?sst=0&sk=1' },
    { label: 'Hepsiburada', url: 'https://www.hepsiburada.com/magaza/ihtiyacin3d' },
  ],
};
