// Environment configuration - Development
// This file will be replaced during production build

// Environment configuration - Development
// This file will be replaced during production build
const localApiBaseUrl = 'https://localhost:7245/api';
let apiVmBaseUrl = 'https://api.horalife.in/api'
export const environment = {
  production: false,
  assetVersion: '0.8.0-dev',
  // Checkout is local-only until billing is deployed. Production API (api.horalife.in) returns 404 for /billing.
  apiBaseUrl: localApiBaseUrl,
  storeBaseUrl: 'http://localhost:4300',
    
  apiTimeout: 30000, // milliseconds
  apiEndpoints: {
    kundali: {
      generate: 'Kundali/generate',
      save: 'Kundali/save',
      getAll: 'Kundali',
      getById: 'Kundali/:id',
      update: 'Kundali/:id',
      delete: 'Kundali/:id',
    },
    compatibility: 'Compatibility',
    panchang: 'Panchang',
    dasha: 'Dasha',
  },

  features: {
    dashboard: true,
    kundali: true,
    compatibility: true,
    panchang: true,
    dasha: true,
  },
};

