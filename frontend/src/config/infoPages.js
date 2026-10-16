// Bilgi sayfaları (`/info/:slug`) — tek yerden düzenlenen statik içerikler.
// İletişim bilgileri siteInfo.js'ten gelir, oradan değişir.
import { siteInfo } from './siteInfo';

const contact = siteInfo;

export const infoPages = {
  hakkimizda: {
    title: 'Hakkımızda',
    blocks: [
      {
        ps: [
          `${contact.storeName}, 3D baskı ile üretilen pratik ürünleri Türkiye'nin her yerine ulaştıran online bir mağazadır.`,
          'İhtiyaca özel tasarım, hızlı üretim ve güvenli alışveriş ilkeleriyle çalışıyoruz.',
        ],
      },
    ],
  },
  iletisim: {
    title: 'Bize Ulaşın',
    blocks: [
      {
        ps: ['Sorularınız için aşağıdaki kanallardan bize ulaşabilirsiniz.'],
        ul: [
          `Adres: ${contact.address}`,
          `Telefon: ${contact.phone}`,
          `E-posta: ${contact.email}`,
          `Destek saatleri: ${contact.workingHours}`,
        ],
      },
      {
        ps: [
          'Siparişinizle ilgili yazıyorsanız destek talebinizde sipariş numaranızı belirtmeniz işlemleri hızlandırır.',
        ],
      },
    ],
  },
  blog: {
    title: 'Blog',
    blocks: [
      {
        ps: [
          'Blog sayfamız çok yakında yayında olacak. Ürün rehberleri ve duyurular burada paylaşılacak.',
        ],
      },
    ],
  },
  kvkk: {
    title: 'KVKK Aydınlatma Metni',
    blocks: [
      {
        ps: [
          `Veri sorumlusu: ${contact.storeName} (${contact.email}).`,
          'Sipariş ve üyelik süreçlerinde paylaştığınız ad-soyad, iletişim ve adres bilgileri; siparişin oluşturulması, teslimatı ve yasal yükümlülüklerin yerine getirilmesi amacıyla işlenir.',
          'Verileriniz açık rızanız olmadan üçüncü kişilerle pazarlama amacıyla paylaşılmaz; kargo ve ödeme kuruluşlarıyla yalnızca hizmetin gerektirdiği ölçüde paylaşılır.',
          'Dilediğiniz zaman verilerinize erişme, düzeltme ve silinmesini isteme hakkına sahipsiniz. Talepleriniz için bize ulaşın.',
        ],
      },
    ],
  },
  'siparis-takibi': {
    title: 'Sipariş Takibi',
    blocks: [
      {
        ps: [
          'Siparişlerinizi hesabınıza giriş yaptıktan sonra Siparişlerim sayfasından takip edebilirsiniz.',
        ],
      },
    ],
  },
  teslimat: {
    title: 'Teslimat Bilgileri',
    blocks: [
      {
        ps: ['Siparişler anlaşmalı kargo firmalarıyla Türkiye geneline gönderilir.'],
        ul: [
          'Stoktaki ürünler 1-3 iş günü içinde kargoya verilir.',
          '3D baskı ile üretilen ürünlerde üretim süresi sepette ve ürün sayfasında belirtilir.',
          `Destek saatleri: ${contact.workingHours}.`,
        ],
      },
    ],
  },
  'iade-iptal': {
    title: 'İade ve İptal',
    blocks: [
      {
        ps: [
          'Kargoya verilmemiş siparişlerinizi ücretsiz iptal edebilirsiniz.',
          'Teslim aldığınız standart ürünleri, kullanılmamış ve yeniden satılabilir durumda olmaları koşuluyla 14 gün içinde iade edebilirsiniz.',
          'Size özel üretilen (kişiselleştirilmiş) ürünlerde, üretim hatası dışında iade kabul edilmez.',
          'İade ve iptal talepleri için sipariş numaranızla bize ulaşın.',
        ],
      },
    ],
  },
  odeme: {
    title: 'Ödeme Bilgileri',
    blocks: [
      {
        ps: ['Ödemelerinizi 256-bit SSL ile korunan altyapı üzerinden güvenle yapabilirsiniz.'],
        ul: [
          'Kredi / banka kartı ile tek çekim ödeme.',
          'Havale / EFT ile ödeme (sipariş sonrası bilgilendirilirsiniz).',
          'Kart bilgileriniz mağazamızda saklanmaz.',
        ],
      },
    ],
  },
  'cerez-politikasi': {
    title: 'Çerez Politikası',
    blocks: [
      {
        ps: [
          'Sitemizde sepet, oturum ve tema tercihleriniz için zorunlu çerezler kullanılır.',
          'Tarayıcı ayarlarınızdan çerezleri dilediğiniz zaman silebilir veya engelleyebilirsiniz; bu durumda sepet ve giriş gibi işlevler çalışmayabilir.',
        ],
      },
    ],
  },
  'mesafeli-satis': {
    title: 'Mesafeli Satış Sözleşmesi',
    blocks: [
      {
        ps: [
          `Satıcı: ${contact.storeName} — ${contact.address} — ${contact.email}.`,
          'Siparişinizi onaylamadan önce ürün bedeli, kargo ücreti ve teslimat süresi ödeme adımında gösterilir.',
          'Cayma hakkı: standart ürünlerde teslimden itibaren 14 gün; kişiselleştirilmiş ürünlerde üretim hatası hariç cayma hakkı kullanılamaz.',
        ],
      },
    ],
  },
  'kullanim-kosullari': {
    title: 'Kullanım Koşulları',
    blocks: [
      {
        ps: [
          'Sitemizi kullanarak ürün bilgilerinin tanıtım amaçlı olduğunu, fiyat ve stokların değişebileceğini kabul etmiş olursunuz.',
          'Hesap güvenliğiniz size aittir; şifrenizi kimseyle paylaşmayın.',
        ],
      },
    ],
  },
  'on-bilgilendirme': {
    title: 'Ön Bilgilendirme Formu',
    blocks: [
      {
        ps: [
          `Satıcı bilgileri: ${contact.storeName}, ${contact.address}, ${contact.email}, ${contact.phone}.`,
          'Ödeme adımında toplam tutar (ürün + kargo) ve tahmini teslim süresi açıkça gösterilir.',
          'Siparişi onaylamanız, bu bilgileri okuduğunuz anlamına gelir.',
        ],
      },
    ],
  },
};
