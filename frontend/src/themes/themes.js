// Tema arayüzü: her tema aynı anahtarlara sahip olmalı.
// Yeni tema eklemek için buraya bir kayıt eklemen yeterli.
// Uyumlu olması için zorunlu anahtarlar: bg, surface, text, muted, primary, primaryText, border, radius, font
export const themes = {
  default: {
    id: 'default',
    name: 'Varsayılan (Açık)',
    vars: {
      '--bg': '#f6f7f9',
      '--surface': '#ffffff',
      '--text': '#1a1d23',
      '--muted': '#6b7280',
      '--primary': '#2563eb',
      '--primary-text': '#ffffff',
      '--border': '#e5e7eb',
      '--radius': '12px',
      '--font': "'Segoe UI', system-ui, sans-serif",
    },
  },
  dark: {
    id: 'dark',
    name: 'Koyu',
    vars: {
      '--bg': '#111318',
      '--surface': '#1c1f26',
      '--text': '#f3f4f6',
      '--muted': '#9ca3af',
      '--primary': '#60a5fa',
      '--primary-text': '#0b1220',
      '--border': '#2e3340',
      '--radius': '12px',
      '--font': "'Segoe UI', system-ui, sans-serif",
    },
  },
  warm: {
    id: 'warm',
    name: 'Sıcak',
    vars: {
      '--bg': '#fdf6ec',
      '--surface': '#ffffff',
      '--text': '#3b2f2f',
      '--muted': '#8a7a6b',
      '--primary': '#c2410c',
      '--primary-text': '#ffffff',
      '--border': '#f0e2cc',
      '--radius': '16px',
      '--font': "'Segoe UI', system-ui, sans-serif",
    },
  },
};
