/**
 * HYSYS AV & RENTALS - ADMIN PANEL CONTROLLER
 */

const API_BASE = window.location.origin;

// State
let authToken = localStorage.getItem('hysys_auth_token') || null;
let currentUser = JSON.parse(localStorage.getItem('hysys_user') || 'null');
let productsCache = [];
let inquiriesCache = [];
let currentTab = 'dashboard';
let currentEditProductId = null;
let currentViewInquiryId = null;

// DOM Elements
const loginView = document.getElementById('loginView');
const adminLayout = document.getElementById('adminLayout');
const loginForm = document.getElementById('loginForm');
const loginError = document.getElementById('loginError');

// Initialization
document.addEventListener('DOMContentLoaded', () => {
  if (authToken) {
    verifySession();
  } else {
    showLogin();
  }

  setupEventListeners();
});

// Setup Events
function setupEventListeners() {
  // Login Form
  if (loginForm) {
    loginForm.addEventListener('submit', handleLogin);
  }

  // Logout Button
  const logoutBtn = document.getElementById('logoutBtn');
  if (logoutBtn) {
    logoutBtn.addEventListener('click', handleLogout);
  }

  // Navigation Items
  document.querySelectorAll('.nav-item[data-tab]').forEach(item => {
    item.addEventListener('click', (e) => {
      e.preventDefault();
      const tab = item.getAttribute('data-tab');
      switchTab(tab);
    });
  });

  // Product Filters & Search
  const productSearch = document.getElementById('productSearch');
  if (productSearch) {
    productSearch.addEventListener('input', debounce(filterProducts, 250));
  }

  const categoryFilter = document.getElementById('categoryFilter');
  if (categoryFilter) {
    categoryFilter.addEventListener('change', filterProducts);
  }

  // Inquiry Filter
  const inquiryFilter = document.getElementById('inquiryFilter');
  if (inquiryFilter) {
    inquiryFilter.addEventListener('change', loadInquiries);
  }

  // Image Upload Previews
  setupImageUpload('productImageInput', 'productImagePreview', 'imageUploadBox');
  setupImageUpload('editProductImageInput', 'editProductImagePreview', 'editImageUploadBox');

  // Product Form Submissions
  const addProductForm = document.getElementById('addProductForm');
  if (addProductForm) {
    addProductForm.addEventListener('submit', handleAddProduct);
  }

  const editProductForm = document.getElementById('editProductForm');
  if (editProductForm) {
    editProductForm.addEventListener('submit', handleEditProduct);
  }

  // Mobile sidebar toggle
  const mobileToggle = document.getElementById('mobileMenuToggle');
  const sidebar = document.querySelector('.admin-sidebar');
  if (mobileToggle && sidebar) {
    mobileToggle.addEventListener('click', () => {
      sidebar.classList.toggle('open');
    });
  }
}

// Session & Auth
async function verifySession() {
  try {
    const res = await fetch(`${API_BASE}/api/auth/me`, {
      headers: { 'Authorization': `Bearer ${authToken}` }
    });

    if (res.ok) {
      const user = await res.json();
      currentUser = user;
      localStorage.setItem('hysys_user', JSON.stringify(user));
      showDashboard();
    } else {
      handleLogout();
    }
  } catch (err) {
    console.error('Session verification error:', err);
    // If backend offline or invalid, fallback to login
    showLogin();
  }
}

async function handleLogin(e) {
  e.preventDefault();
  loginError.style.display = 'none';
  const submitBtn = loginForm.querySelector('button[type="submit"]');
  const originalText = submitBtn.innerHTML;

  const username = document.getElementById('usernameInput').value.trim();
  const password = document.getElementById('passwordInput').value;

  try {
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span>Authenticating...</span>';

    const res = await fetch(`${API_BASE}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    const data = await res.json();

    if (!res.ok) {
      throw new Error(data.message || 'Authentication failed. Please check credentials.');
    }

    authToken = data.token;
    currentUser = {
      username: data.username,
      fullName: data.fullName,
      role: data.role
    };

    localStorage.setItem('hysys_auth_token', authToken);
    localStorage.setItem('hysys_user', JSON.stringify(currentUser));

    showToast('Login successful! Welcome to HYSYS Admin.', 'success');
    showDashboard();
  } catch (err) {
    loginError.innerText = err.message;
    loginError.style.display = 'block';
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = originalText;
  }
}

function handleLogout() {
  authToken = null;
  currentUser = null;
  localStorage.removeItem('hysys_auth_token');
  localStorage.removeItem('hysys_user');
  showLogin();
  showToast('Logged out successfully.', 'info');
}

function showLogin() {
  if (loginView) loginView.style.display = 'flex';
  if (adminLayout) adminLayout.style.display = 'none';
}

function showDashboard() {
  if (loginView) loginView.style.display = 'none';
  if (adminLayout) adminLayout.style.display = 'flex';

  // Update user display
  const nameEl = document.getElementById('adminDisplayName');
  const avatarEl = document.getElementById('adminAvatarInitial');
  if (nameEl && currentUser) nameEl.innerText = currentUser.fullName || currentUser.username;
  if (avatarEl && currentUser) avatarEl.innerText = (currentUser.fullName || currentUser.username).charAt(0).toUpperCase();

  switchTab('dashboard');
}

// Navigation Tabs
function switchTab(tabName) {
  currentTab = tabName;

  // Update Nav Active state
  document.querySelectorAll('.nav-item').forEach(item => {
    if (item.getAttribute('data-tab') === tabName) {
      item.classList.add('active');
    } else {
      item.classList.remove('active');
    }
  });

  // Show Section
  document.querySelectorAll('.content-section').forEach(sec => sec.classList.remove('active'));
  const target = document.getElementById(`section-${tabName}`);
  if (target) target.classList.add('active');

  // Update Topbar Title
  const titleEl = document.getElementById('pageTitle');
  if (titleEl) {
    const titles = {
      dashboard: 'Dashboard Overview',
      products: 'Store & Products Management',
      inquiries: 'Customer Contact Inquiries'
    };
    titleEl.innerText = titles[tabName] || 'Admin Panel';
  }

  // Load Data
  if (tabName === 'dashboard') {
    loadDashboardData();
  } else if (tabName === 'products') {
    loadProducts();
  } else if (tabName === 'inquiries') {
    loadInquiries();
  }
}

// Dashboard Data
async function loadDashboardData() {
  await Promise.all([loadProducts(), loadInquiries()]);
  
  // Calculate statistics
  const totalProducts = productsCache.length;
  const totalInquiries = inquiriesCache.length;
  const unreadInquiries = inquiriesCache.filter(i => !i.isRead).length;
  const categories = new Set(productsCache.map(p => p.category)).size;

  document.getElementById('statTotalProducts').innerText = totalProducts;
  document.getElementById('statTotalInquiries').innerText = totalInquiries;
  document.getElementById('statUnreadInquiries').innerText = unreadInquiries;
  document.getElementById('statCategories').innerText = categories;

  // Update unread badge in sidebar
  const badge = document.getElementById('inquiriesBadge');
  if (badge) {
    if (unreadInquiries > 0) {
      badge.innerText = unreadInquiries;
      badge.style.display = 'inline-block';
    } else {
      badge.style.display = 'none';
    }
  }

  // Render recent products & recent inquiries on overview
  renderRecentDashboardLists();
}

function renderRecentDashboardLists() {
  const recentProductsList = document.getElementById('dashboardRecentProducts');
  if (recentProductsList) {
    const recent = productsCache.slice(0, 4);
    if (recent.length === 0) {
      recentProductsList.innerHTML = '<p class="text-muted" style="padding: 16px;">No products added yet.</p>';
    } else {
      recentProductsList.innerHTML = recent.map(p => `
        <div style="display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid var(--border-color);">
          <div style="display: flex; align-items: center; gap: 12px;">
            <img src="${API_BASE}${p.imageUrl}" alt="${p.name}" style="width: 36px; height: 36px; border-radius: 6px; object-fit: cover;" onerror="this.src='https://via.placeholder.com/40'">
            <div>
              <div style="font-weight: 600; font-size: 0.88rem;">${escapeHtml(p.name)}</div>
              <div style="font-size: 0.76rem; color: var(--text-dim);">${escapeHtml(p.category)} • ₹${p.price.toLocaleString()}</div>
            </div>
          </div>
          <span class="badge badge-in-stock">${escapeHtml(p.stockStatus || 'In Stock')}</span>
        </div>
      `).join('');
    }
  }

  const recentInquiriesList = document.getElementById('dashboardRecentInquiries');
  if (recentInquiriesList) {
    const recent = inquiriesCache.slice(0, 4);
    if (recent.length === 0) {
      recentInquiriesList.innerHTML = '<p class="text-muted" style="padding: 16px;">No inquiries received yet.</p>';
    } else {
      recentInquiriesList.innerHTML = recent.map(i => `
        <div style="display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid var(--border-color); cursor: pointer;" onclick="openInquiryModal(${i.id})">
          <div>
            <div style="font-weight: 600; font-size: 0.88rem; color: ${i.isRead ? 'var(--text-main)' : '#38bdf8'};">
              ${escapeHtml(i.name)} ${i.isRead ? '' : '●'}
            </div>
            <div style="font-size: 0.76rem; color: var(--text-dim);">${escapeHtml(i.email)} • ${new Date(i.createdAt).toLocaleDateString()}</div>
          </div>
          <span class="badge ${i.isRead ? 'badge-read' : 'badge-unread'}">${i.isRead ? 'Read' : 'New'}</span>
        </div>
      `).join('');
    }
  }
}

// Product Management
async function loadProducts() {
  const tbody = document.getElementById('productsTableBody');
  if (tbody) tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 30px;">Loading products...</td></tr>';

  try {
    const res = await fetch(`${API_BASE}/api/products`);
    if (!res.ok) throw new Error('Failed to load products');

    productsCache = await res.json();
    populateCategoryFilter();
    renderProductsTable(productsCache);
  } catch (err) {
    console.error('Error fetching products:', err);
    if (tbody) tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--danger); padding: 30px;">Error loading products: ${err.message}</td></tr>`;
  }
}

function populateCategoryFilter() {
  const select = document.getElementById('categoryFilter');
  if (!select) return;

  const current = select.value;
  const categories = Array.from(new Set(productsCache.map(p => p.category).filter(Boolean)));

  select.innerHTML = '<option value="all">All Categories</option>' + 
    categories.map(cat => `<option value="${escapeHtml(cat)}">${escapeHtml(cat)}</option>`).join('');

  select.value = current || 'all';
}

function filterProducts() {
  const searchTerm = (document.getElementById('productSearch')?.value || '').toLowerCase().trim();
  const category = document.getElementById('categoryFilter')?.value || 'all';

  const filtered = productsCache.filter(p => {
    const matchesSearch = !searchTerm || 
      p.name.toLowerCase().includes(searchTerm) || 
      (p.description && p.description.toLowerCase().includes(searchTerm));
    
    const matchesCategory = category === 'all' || p.category === category;

    return matchesSearch && matchesCategory;
  });

  renderProductsTable(filtered);
}

function renderProductsTable(products) {
  const tbody = document.getElementById('productsTableBody');
  if (!tbody) return;

  if (products.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="6">
          <div class="empty-state">
            <svg width="48" height="48" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/></svg>
            <h3>No products found</h3>
            <p>Add your first store product or adjust your filters.</p>
          </div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = products.map(p => `
    <tr>
      <td>
        <div class="product-cell">
          <img src="${API_BASE}${p.imageUrl}" alt="${escapeHtml(p.name)}" class="product-thumb" onerror="this.src='https://via.placeholder.com/52?text=Product'">
          <div class="product-meta">
            <h4>${escapeHtml(p.name)}</h4>
            <p title="${escapeHtml(p.description)}">${escapeHtml(p.description || 'No description provided')}</p>
          </div>
        </div>
      </td>
      <td>
        <span class="badge badge-category">${escapeHtml(p.category || 'General')}</span>
      </td>
      <td>
        <span class="price-text">₹${Number(p.price).toLocaleString()}</span>
      </td>
      <td>
        <span class="badge ${p.stockStatus === 'Built to Order' ? 'badge-order' : 'badge-in-stock'}">
          ${escapeHtml(p.stockStatus || 'In Stock')}
        </span>
      </td>
      <td style="color: var(--text-dim); font-size: 0.8rem;">
        ${p.createdAt ? new Date(p.createdAt).toLocaleDateString() : 'N/A'}
      </td>
      <td>
        <div class="action-buttons">
          <button class="btn-icon edit" title="Edit Product" onclick="openEditProductModal(${p.id})">
            <svg width="15" height="15" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
          </button>
          <button class="btn-icon delete" title="Delete Product" onclick="confirmDeleteProduct(${p.id}, '${escapeHtml(p.name)}')">
            <svg width="15" height="15" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
          </button>
        </div>
      </td>
    </tr>
  `).join('');
}

// Add Product Modal & Submission
function openAddProductModal() {
  document.getElementById('addProductForm').reset();
  const preview = document.getElementById('productImagePreview');
  if (preview) {
    preview.src = '';
    preview.style.display = 'none';
  }
  openModal('addProductModal');
}

async function handleAddProduct(e) {
  e.preventDefault();
  const form = e.target;
  const submitBtn = form.querySelector('button[type="submit"]');
  const originalText = submitBtn.innerHTML;

  const formData = new FormData(form);

  // Validate image
  const imageFile = formData.get('Image');
  if (!imageFile || imageFile.size === 0) {
    showToast('Please select a product image.', 'error');
    return;
  }

  try {
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span>Uploading & Saving...</span>';

    const res = await fetch(`${API_BASE}/api/products`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${authToken}`
      },
      body: formData
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to add product');

    showToast('Product added successfully!', 'success');
    closeModal('addProductModal');
    loadProducts();
    if (currentTab === 'dashboard') loadDashboardData();
  } catch (err) {
    showToast(err.message, 'error');
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = originalText;
  }
}

// Edit Product Modal & Submission
function openEditProductModal(id) {
  const product = productsCache.find(p => p.id === id);
  if (!product) return;

  currentEditProductId = id;
  document.getElementById('editProductId').value = product.id;
  document.getElementById('editProductName').value = product.name;
  document.getElementById('editProductCategory').value = product.category;
  document.getElementById('editProductPrice').value = product.price;
  document.getElementById('editProductStock').value = product.stockStatus || 'In Stock';
  document.getElementById('editProductDesc').value = product.description || '';

  const preview = document.getElementById('editProductImagePreview');
  if (preview) {
    preview.src = `${API_BASE}${product.imageUrl}`;
    preview.style.display = 'block';
  }

  openModal('editProductModal');
}

async function handleEditProduct(e) {
  e.preventDefault();
  if (!currentEditProductId) return;

  const form = e.target;
  const submitBtn = form.querySelector('button[type="submit"]');
  const originalText = submitBtn.innerHTML;

  const formData = new FormData(form);

  try {
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span>Updating...</span>';

    const res = await fetch(`${API_BASE}/api/products/${currentEditProductId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${authToken}`
      },
      body: formData
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to update product');

    showToast('Product updated successfully!', 'success');
    closeModal('editProductModal');
    loadProducts();
  } catch (err) {
    showToast(err.message, 'error');
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = originalText;
  }
}

// Delete Product
function confirmDeleteProduct(id, name) {
  if (confirm(`Are you sure you want to delete "${name}"? This action cannot be undone.`)) {
    deleteProduct(id);
  }
}

async function deleteProduct(id) {
  try {
    const res = await fetch(`${API_BASE}/api/products/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    });

    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Failed to delete product');

    showToast('Product deleted successfully.', 'success');
    loadProducts();
    if (currentTab === 'dashboard') loadDashboardData();
  } catch (err) {
    showToast(err.message, 'error');
  }
}

// Inquiries / Contact Submissions
async function loadInquiries() {
  const tbody = document.getElementById('inquiriesTableBody');
  const unreadOnly = document.getElementById('inquiryFilter')?.value === 'unread';

  if (tbody) tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 30px;">Loading inquiries...</td></tr>';

  try {
    const url = unreadOnly ? `${API_BASE}/api/contact?unreadOnly=true` : `${API_BASE}/api/contact`;
    const res = await fetch(url, {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    });

    if (!res.ok) throw new Error('Failed to load inquiries');

    inquiriesCache = await res.json();
    renderInquiriesTable(inquiriesCache);
  } catch (err) {
    console.error('Error fetching inquiries:', err);
    if (tbody) tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--danger); padding: 30px;">Error loading inquiries: ${err.message}</td></tr>`;
  }
}

function renderInquiriesTable(inquiries) {
  const tbody = document.getElementById('inquiriesTableBody');
  if (!tbody) return;

  if (inquiries.length === 0) {
    tbody.innerHTML = `
      <tr>
        <td colspan="6">
          <div class="empty-state">
            <svg width="48" height="48" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/></svg>
            <h3>No inquiries found</h3>
            <p>Customer contact form submissions will appear here.</p>
          </div>
        </td>
      </tr>
    `;
    return;
  }

  tbody.innerHTML = inquiries.map(i => `
    <tr style="${i.isRead ? '' : 'background: rgba(59, 130, 246, 0.04); font-weight: 600;'}">
      <td>
        <span class="badge ${i.isRead ? 'badge-read' : 'badge-unread'}">
          ${i.isRead ? 'Read' : 'New'}
        </span>
      </td>
      <td>
        <div style="font-weight: 600; color: #fff;">${escapeHtml(i.name)}</div>
        <div style="font-size: 0.78rem; color: var(--text-dim);">${escapeHtml(i.email)}</div>
      </td>
      <td>
        <div style="font-size: 0.88rem;">${escapeHtml(i.phone || '—')}</div>
      </td>
      <td>
        <div style="font-weight: 500; max-width: 250px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
          ${escapeHtml(i.subject || 'No Subject')}
        </div>
        <div style="font-size: 0.78rem; color: var(--text-dim); max-width: 250px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">
          ${escapeHtml(i.message)}
        </div>
      </td>
      <td style="color: var(--text-dim); font-size: 0.8rem;">
        ${new Date(i.createdAt).toLocaleString()}
      </td>
      <td>
        <div class="action-buttons">
          <button class="btn-icon view" title="View Message" onclick="openInquiryModal(${i.id})">
            <svg width="15" height="15" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>
          </button>
          <button class="btn-icon delete" title="Delete Inquiry" onclick="confirmDeleteInquiry(${i.id}, '${escapeHtml(i.name)}')">
            <svg width="15" height="15" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
          </button>
        </div>
      </td>
    </tr>
  `).join('');
}

async function openInquiryModal(id) {
  const inquiry = inquiriesCache.find(i => i.id === id);
  if (!inquiry) return;

  currentViewInquiryId = id;
  document.getElementById('inquiryDetailName').innerText = inquiry.name;
  document.getElementById('inquiryDetailEmail').innerText = inquiry.email;
  document.getElementById('inquiryDetailPhone').innerText = inquiry.phone || 'Not provided';
  document.getElementById('inquiryDetailSubject').innerText = inquiry.subject || 'No Subject';
  document.getElementById('inquiryDetailDate').innerText = new Date(inquiry.createdAt).toLocaleString();
  document.getElementById('inquiryDetailMessage').innerText = inquiry.message;

  const replyBtn = document.getElementById('inquiryReplyBtn');
  if (replyBtn) {
    replyBtn.href = `mailto:${inquiry.email}?subject=Re: ${encodeURIComponent(inquiry.subject || 'Your Inquiry to HYSYS')}`;
  }

  const markBtn = document.getElementById('inquiryMarkReadBtn');
  if (markBtn) {
    markBtn.innerText = inquiry.isRead ? 'Mark as Unread' : 'Mark as Read';
    markBtn.onclick = () => toggleInquiryReadStatus(id, !inquiry.isRead);
  }

  // Automatically mark as read if opened while unread
  if (!inquiry.isRead) {
    toggleInquiryReadStatus(id, true, false);
  }

  openModal('inquiryModal');
}

async function toggleInquiryReadStatus(id, isRead, refreshUI = true) {
  try {
    await fetch(`${API_BASE}/api/contact/${id}/read?isRead=${isRead}`, {
      method: 'PATCH',
      headers: { 'Authorization': `Bearer ${authToken}` }
    });

    const inq = inquiriesCache.find(i => i.id === id);
    if (inq) inq.isRead = isRead;

    if (refreshUI) {
      loadInquiries();
      closeModal('inquiryModal');
    }
  } catch (err) {
    console.error('Error toggling inquiry read status:', err);
  }
}

function confirmDeleteInquiry(id, name) {
  if (confirm(`Delete inquiry from "${name}"?`)) {
    deleteInquiry(id);
  }
}

async function deleteInquiry(id) {
  try {
    const res = await fetch(`${API_BASE}/api/contact/${id}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${authToken}` }
    });

    if (!res.ok) throw new Error('Failed to delete inquiry');

    showToast('Inquiry deleted successfully.', 'success');
    closeModal('inquiryModal');
    loadInquiries();
    if (currentTab === 'dashboard') loadDashboardData();
  } catch (err) {
    showToast(err.message, 'error');
  }
}

// Image Upload Preview Helper
function setupImageUpload(inputId, previewId, boxId) {
  const input = document.getElementById(inputId);
  const preview = document.getElementById(previewId);
  const box = document.getElementById(boxId);

  if (!input || !preview || !box) return;

  box.addEventListener('click', () => input.click());

  box.addEventListener('dragover', (e) => {
    e.preventDefault();
    box.classList.add('dragover');
  });

  box.addEventListener('dragleave', () => box.classList.remove('dragover'));

  box.addEventListener('drop', (e) => {
    e.preventDefault();
    box.classList.remove('dragover');
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      input.files = e.dataTransfer.files;
      displayPreview(e.dataTransfer.files[0]);
    }
  });

  input.addEventListener('change', () => {
    if (input.files && input.files[0]) {
      displayPreview(input.files[0]);
    }
  });

  function displayPreview(file) {
    const reader = new FileReader();
    reader.onload = (e) => {
      preview.src = e.target.result;
      preview.style.display = 'block';
    };
    reader.readAsDataURL(file);
  }
}

// Modal Helpers
function openModal(id) {
  const modal = document.getElementById(id);
  if (modal) modal.classList.add('active');
}

function closeModal(id) {
  const modal = document.getElementById(id);
  if (modal) modal.classList.remove('active');
}

// Toast Notifications
function showToast(message, type = 'info') {
  let container = document.querySelector('.toast-container');
  if (!container) {
    container = document.createElement('div');
    container.className = 'toast-container';
    document.body.appendChild(container);
  }

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.innerText = message;

  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(40px)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

// Utilities
function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

// Change Password Modal Handler
function openChangePasswordModal() {
  const form = document.getElementById('changePasswordForm');
  if (form) form.reset();
  const err = document.getElementById('changePwdError');
  const succ = document.getElementById('changePwdSuccess');
  if (err) err.style.display = 'none';
  if (succ) succ.style.display = 'none';
  openModal('changePasswordModal');
}

const changePwdForm = document.getElementById('changePasswordForm');
if (changePwdForm) {
  changePwdForm.addEventListener('submit', async function (e) {
    e.preventDefault();
    const cur = document.getElementById('currentPassword').value;
    const newPwd = document.getElementById('newPassword').value;
    const conf = document.getElementById('confirmNewPassword').value;
    const err = document.getElementById('changePwdError');
    const succ = document.getElementById('changePwdSuccess');
    const submitBtn = document.getElementById('savePasswordBtn');

    if (err) err.style.display = 'none';
    if (succ) succ.style.display = 'none';

    if (newPwd !== conf) {
      if (err) {
        err.innerText = 'New password and confirmation password do not match.';
        err.style.display = 'block';
      }
      return;
    }

    if (newPwd.length < 6) {
      if (err) {
        err.innerText = 'New password must be at least 6 characters.';
        err.style.display = 'block';
      }
      return;
    }

    try {
      submitBtn.disabled = true;
      submitBtn.innerText = 'Updating...';

      const res = await fetch(`${API_BASE}/api/auth/change-password`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`
        },
        body: JSON.stringify({
          currentPassword: cur,
          newPassword: newPwd,
          confirmNewPassword: conf
        })
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.message || 'Failed to update password.');

      if (succ) {
        succ.innerText = data.message || 'Password changed successfully!';
        succ.style.display = 'block';
      }
      showToast('Password updated successfully!', 'success');
      setTimeout(() => {
        closeModal('changePasswordModal');
      }, 1500);
    } catch (error) {
      if (err) {
        err.innerText = error.message;
        err.style.display = 'block';
      }
    } finally {
      submitBtn.disabled = false;
      submitBtn.innerText = 'Update Password';
    }
  });
}
