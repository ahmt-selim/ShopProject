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
  if (!res.ok) throw new Error(data.error || `API error ${res.status}`);
  return data;
}

export const STATUSES = ['Hazırlanıyor', 'Kargoda', 'Teslim Edildi', 'İptal Edildi'];

export const statusClass = (s) =>
  s === 'Kargoda' ? 'kargo' : s === 'Teslim Edildi' ? 'ok' : s?.startsWith('İptal') ? 'iptal' : 'hazir';

export const api = {
  getProducts: (categoryId, q) => {
    const params = new URLSearchParams();
    if (categoryId) params.set('categoryId', categoryId);
    if (q) params.set('q', q);
    const qs = params.toString();
    return request(`/api/products${qs ? `?${qs}` : ''}`);
  },
  getProduct: (id) => request(`/api/products/${id}`),
  getCategories: () => request('/api/categories'),
  getBrands: () => request('/api/brands'),
  getPublicSettings: () => request('/api/settings/public'),

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
  getMyOrder: (id) => request(`/api/orders/mine/${id}`),
  getOrders: () => request('/api/orders'),
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

  imageUrl: fullUrl,

  primaryImage: (p) =>
    p?.images?.length
      ? fullUrl(p.images[0].url)
      : `https://picsum.photos/seed/product-${p?.id || 'x'}/600/400`,
};
