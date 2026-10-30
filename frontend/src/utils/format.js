export function formatMoney(value) {
  const n = Number(value ?? 0);
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
}

export function formatDateTime(value) {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleString('tr-TR', { timeZone: 'Europe/Istanbul' });
  } catch {
    return String(value);
  }
}

export function formatDate(value) {
  if (!value) return '-';
  try {
    return new Date(value).toLocaleDateString('tr-TR', { timeZone: 'Europe/Istanbul' });
  } catch {
    return String(value);
  }
}