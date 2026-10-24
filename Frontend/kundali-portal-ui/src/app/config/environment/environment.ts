// Environment configuration - Development
// This file will be replaced during production build

// Environment configuration - Development
// This file will be replaced during production build
const localApiBaseUrl = 'https://localhost:7245/api';
let apiVmBaseUrl = 'https://api.horalife.in/api'
let storelocalBaseUrl = 'http://localhost:4300';
let storeVmBaseUrl = 'https://store.horalife.in';
export const environment = {
  production: false,
  assetVersion: '0.8.0-dev',
  // Google sign-in exists on the local API. api.horalife.in does not have this route until that build is deployed.
  apiBaseUrl: localApiBaseUrl,
  storeBaseUrl: storelocalBaseUrl,
  googleClientId: '560889779781-ap5tq1rl8p50aoe9qee4mbcgkr0i3v0i.apps.googleusercontent.com',
    
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

