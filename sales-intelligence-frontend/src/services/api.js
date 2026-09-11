import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5082/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Response Interceptor
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('user');
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

// Auth Service
export const authService = {
  login: async (email, password) => {
    const res = await api.post('/auth/login', { email, password });
    if (res.data) {
      localStorage.setItem('user', JSON.stringify(res.data));
    }
    return res.data;
  },
  logout: () => {
    localStorage.removeItem('user');
    window.location.href = '/login';
  },
  getCurrentUser: () => {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  },
};

// Dashboard Service
export const dashboardService = {
  getKpis: async (params = {}) => {
    const res = await api.get('/dashboard/kpis', { params });
    return res.data;
  },
};

// Deals Service
export const dealsService = {
  getDeals: async (params) => {
    const res = await api.get('/deals', { params });
    return res.data;
  },
  getDealById: async (id) => {
    const res = await api.get(`/deals/${id}`);
    return res.data;
  },
  createDeal: async (dealData) => {
    const res = await api.post('/deals', dealData);
    return res.data;
  },
};

// Accounts Service
export const accountsService = {
  getAccounts: async (params) => {
    const res = await api.get('/accounts', { params });
    return res.data;
  },
  getAccountById: async (id) => {
    const res = await api.get(`/accounts/${id}`);
    return res.data;
  },
};


// Datasets Service
export const datasetsService = {
  getDatasets: async (params = {}) => {
    const res = await api.get('/datasets', { params });
    return res.data;
  },
  uploadDataset: async (formData) => {
    const res = await api.post('/datasets/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  },
  deleteDataset: async (id, params = {}) => {
    const res = await api.delete(`/datasets/${id}`, { params });
    return res.data;
  },
};

// Agents Service
export const agentsService = {
  getAgents: async (params) => {
    const res = await api.get('/agents', { params });
    return res.data;
  },
  getAgentPerformance: async (id, params = {}) => {
    const res = await api.get(`/agents/${id}/performance`, { params });
    return res.data;
  },
};

// Predictions Service
export const predictionsService = {
  predictCompanyRevenue: async (data) => {
    const res = await api.post('/predictions/company-revenue', data);
    return res.data;
  },
  predictWinRate: async (data) => {
    const res = await api.post('/predictions/win-rate', data);
    return res.data;
  },
  predictEmployeePerformance: async (data) => {
    const res = await api.post('/predictions/employee-performance', data);
    return res.data;
  },
  predictEmployeeRevenue: async (data) => {
    const res = await api.post('/predictions/employee-revenue', data);
    return res.data;
  },
  getHistory: async () => {
    const res = await api.get('/predictions/history');
    return res.data;
  },
};

// User Management Service
export const userService = {
  getUsers: async (params) => {
    const res = await api.get('/users', { params });
    return res.data;
  },
  createUser: async (userData) => {
    const res = await api.post('/users', userData);
    return res.data;
  },
  updateUser: async (id, userData) => {
    const res = await api.put(`/users/${id}`, userData);
    return res.data;
  },
  deleteUser: async (id) => {
    const res = await api.delete(`/users/${id}`);
    return res.data;
  },
};

// Agent Service (Groq SQL Agent)
export const agentService = {
  ask: async (question, datasetId = null) => {
    const res = await api.post('/agent/ask', { question, datasetId });
    return res.data;
  },
};

// Chat Service
export const chatService = {
  sendMessage: async (message, datasetId = null) => {
    const res = await api.post('/agent/ask', { question: message, datasetId });
    return res.data;
  },
};

export default api;

