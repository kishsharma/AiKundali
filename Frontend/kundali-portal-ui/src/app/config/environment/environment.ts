// Environment configuration - Development
// This file will be replaced during production build

// Environment configuration - Development
// This file will be replaced during production build
let apiVmBaseUrl = 'https://api.horalife.in/api'
let localApiBaseUrl='https://localhost:7245/api'
export const environment = {
  production: false,
  apiBaseUrl: apiVmBaseUrl,
    
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

