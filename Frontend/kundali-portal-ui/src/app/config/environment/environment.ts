// Environment configuration - Development
// This file will be replaced during production build

// Environment configuration - Development
// This file will be replaced during production build

export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7245/api',
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

