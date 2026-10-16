const BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

function authHeaders() {
  const token = localStorage.getItem('token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function fullUrl(path) {
  if (!path) return '';
  if (path.startsWith('http')) return path;
  if (path.startsWith('/uploads/')) return `${BASE_URL}${path}`;
  return path;
}

async function request(path, options = {}) {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
      ...authHeaders(),
      ...(options.headers || {}),
    },
  });
  if (res.status === 204) return null;
  const data = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error(extractError(data, res.status));
  return data;
}

// Eski { error } formatı + ProblemDetails (title/detail/errors) uyumlu hata metni.
function extractError(data, status) {
  if (data?.error) return data.error;
  if (data?.errors && typeof data.errors === 'object') {
    const first = Object.values(data.errors).flat().find(Boolean);
    if (first) return String(first);
  }
  if (data?.detail && data.detail !== data?.title) return data.detail;
  if (data?.title) return data.title;
  return `API error ${status}`;
}

export const STATUSES = ['Hazırlanıyor', 'Kargoda', 'Teslim Edildi', 'İptal Edildi'];

export const statusClass = (s) =>
  s === 'Kargoda' ? 'kargo' : s === 'Teslim Edildi' ? 'ok' : s?.startsWith('İptal') ? 'iptal' : 'hazir';

export const api = {
  getProducts: (categoryId, q, page, pageSize) => {
    const params = new URLSearchParams();
    if (categoryId) params.set('categoryId', categoryId);
    if (q) params.set('q', q);
    if (page) params.set('page', page);
    if (pageSize) params.set('pageSize', pageSize);
    const qs = params.toString();
    return request(`/api/products${qs ? `?${qs}` : ''}`);
  },
  getProduct: (id) => request(`/api/products/${id}`),
  getCategories: (page, pageSize) => {
    const qs = page ? `?page=${page}&pageSize=${pageSize || 20}` : '';
    return request(`/api/categories${qs}`);
  },
  getBrands: (page, pageSize) => {
    const qs = page ? `?page=${page}&pageSize=${pageSize || 20}` : '';
    return request(`/api/brands${qs}`);
  },
  getPublicSettings: () => request('/api/settings/public'),

  _publicCache: null,
  publicSettings() {
    if (!this._publicCache) this._publicCache = this.getPublicSettings().catch(() => ({}));
    return this._publicCache;
  },
  freeShipping: async (price) => {
    const s = await api.publicSettings();
    const limit = Number(s.freeShippingLimit || 0);
    return limit > 0 && Number(price) >= limit;
  },

  getProductImages: (productId) => request(`/api/products/${productId}/images`),
  addProductImage: (productId, url) =>
    request(`/api/products/${productId}/images`, { method: 'POST', body: JSON.stringify({ url }) }),
  uploadProductImage: async (productId, file) => {
    const form = new FormData();
    form.append('file', file);
    const res = await fetch(`${BASE_URL}/api/products/${productId}/images`, {
      method: 'POST',
      headers: authHeaders(),
      body: form,
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || `API error ${res.status}`);
    return data;
  },
  deleteProductImage: (imageId) => request(`/api/product-images/${imageId}`, { method: 'DELETE' }),

  register: (email, password, name) =>
    request('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, password, name }) }),
  login: (email, password) =>
    request('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  me: () => request('/api/auth/me'),
  updateProfile: (data) =>
    request('/api/auth/profile', { method: 'PUT', body: JSON.stringify(data) }),

  getAddresses: () => request('/api/addresses'),
  addAddress: (data) =>
    request('/api/addresses', { method: 'POST', body: JSON.stringify(data) }),
  updateAddress: (id, data) =>
    request(`/api/addresses/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteAddress: (id) => request(`/api/addresses/${id}`, { method: 'DELETE' }),

  getFavorites: () => request('/api/favorites'),
  addFavorite: (productId) =>
    request('/api/favorites', { method: 'POST', body: JSON.stringify({ productId }) }),
  removeFavorite: (productId) => request(`/api/favorites/${productId}`, { method: 'DELETE' }),

  getCart: () => request('/api/cart'),
  addCartItem: (productId, qty) =>
    request('/api/cart/items', { method: 'POST', body: JSON.stringify({ productId, qty }) }),
  setCartItem: (productId, qty) =>
    request(`/api/cart/items/${productId}`, { method: 'PUT', body: JSON.stringify({ productId, qty }) }),
  removeCartItem: (productId) => request(`/api/cart/items/${productId}`, { method: 'DELETE' }),
  clearCart: () => request('/api/cart', { method: 'DELETE' }),

  createProduct: (data) =>
    request('/api/products', { method: 'POST', body: JSON.stringify(data) }),
  updateProduct: (id, data) =>
    request(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteProduct: (id) => request(`/api/products/${id}`, { method: 'DELETE' }),

  createCategory: (name) =>
    request('/api/categories', { method: 'POST', body: JSON.stringify({ name }) }),
  updateCategory: (id, name) =>
    request(`/api/categories/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }),
  deleteCategory: (id) => request(`/api/categories/${id}`, { method: 'DELETE' }),

  createBrand: (name) =>
    request('/api/brands', { method: 'POST', body: JSON.stringify({ name }) }),
  updateBrand: (id, name) =>
    request(`/api/brands/${id}`, { method: 'PUT', body: JSON.stringify({ name }) }),
  deleteBrand: (id) => request(`/api/brands/${id}`, { method: 'DELETE' }),

  checkout: (addressId, items) =>
    request('/api/orders/checkout', { method: 'POST', body: JSON.stringify({ addressId, items }) }),
  getMyOrders: () => request('/api/orders/mine'),
  getMyOrder: (id) => request(`/api/orders/mine/${id}`),  getOrders: (page, pageSize) => {
    const qs = page ? `?page=${page}&pageSize=${pageSize || 20}` : '';
    return request(`/api/orders${qs}`);
  },
  getOrder: (id) => request(`/api/orders/${id}`),
  setOrderStatus: (id, status) =>
    request(`/api/orders/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) }),

  getCustomers: () => request('/api/customers'),
  getCustomer: (id) => request(`/api/customers/${id}`),

  getSettings: () => request('/api/settings'),
  saveSettings: (values) =>
    request('/api/settings', { method: 'PUT', body: JSON.stringify(values) }),

  uploadImage: async (file) => {
    const form = new FormData();
    form.append('file', file);
    const data = await request('/api/upload', { method: 'POST', body: form });
    return fullUrl(data.url);
  },

  getImportTemplates: () => request('/api/importtemplates'),
  saveImportTemplate: (name, mappings, startRow, matchBy, fileName) =>
    request('/api/importtemplates', { method: 'POST', body: JSON.stringify({ name, mappings, startRow, matchBy, fileName }) }),
  updateImportTemplate: (id, name, mappings, startRow, matchBy, fileName) =>
    request(`/api/importtemplates/${id}`, { method: 'PUT', body: JSON.stringify({ name, mappings, startRow, matchBy, fileName }) }),
  deleteImportTemplate: (id) => request(`/api/importtemplates/${id}`, { method: 'DELETE' }),
  importProducts: async (file, { templateId, mappings, startRow, matchBy }) => {
    const form = new FormData();
    if (file) form.append('file', file);
    if (templateId) form.append('templateId', templateId);
    if (mappings) form.append('mappings', JSON.stringify(mappings));
    if (startRow) form.append('startRow', startRow);
    if (matchBy) form.append('matchBy', matchBy);
    const res = await fetch(`${BASE_URL}/api/products/import`, {
      method: 'POST',
      headers: authHeaders(),
      body: form,
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.error || `API error ${res.status}`);
    return data;
  },

  // --- Yorumlar (ReviewDto, ReviewSummaryDto, EligibilityDto: backend Dtos/Reviews) ---
  getReviews: (productId, { page, pageSize, sort, rating, withImages } = {}) => {
    const params = new URLSearchParams();
    if (page) params.set('page', page);
    if (pageSize) params.set('pageSize', pageSize);
    if (sort) params.set('sort', sort);
    if (rating) params.set('rating', rating);
    if (withImages) params.set('withImages', 'true');
    const qs = params.toString();
    return request(`/api/products/${productId}/reviews${qs ? `?${qs}` : ''}`);
  },
  getReviewSummary: (productId) => request(`/api/products/${productId}/reviews/summary`),
  getReviewEligibility: (productId) => request(`/api/products/${productId}/reviews/eligibility`),
  getMyReview: (productId) => request(`/api/products/${productId}/reviews/mine`),
  createReview: (productId, { rating, title, comment, images }) => {
    const form = new FormData();
    form.append('rating', rating);
    if (title?.trim()) form.append('title', title.trim());
    if (comment?.trim()) form.append('comment', comment.trim());
    (images || []).forEach((f) => form.append('images', f));
    return request(`/api/products/${productId}/reviews`, { method: 'POST', body: form });
  },
  updateReview: (id, { rating, title, comment, keepImageIds, images }) => {
    const form = new FormData();
    form.append('rating', rating);
    if (title?.trim()) form.append('title', title.trim());
    if (comment?.trim()) form.append('comment', comment.trim());
    (keepImageIds || []).forEach((imgId) => form.append('keepImageIds', imgId));
    (images || []).forEach((f) => form.append('images', f));
    return request(`/api/reviews/${id}`, { method: 'PUT', body: form });
  },
  deleteReview: (id) => request(`/api/reviews/${id}`, { method: 'DELETE' }),

  imageUrl: fullUrl,

  primaryImage: (p) =>
    p?.images?.length
      ? fullUrl(p.images[0].url)
      : `https://picsum.photos/seed/product-${p?.id || 'x'}/600/400`,
};
