// Environment configuration - Staging

export const environment = {
  production: false,
  assetVersion: '0.8.1-outlook',
  apiBaseUrl: 'https://staging-api.kundali-portal.com/api',
  storeBaseUrl: 'https://store.horalife.in',
  apiTimeout: 30000,
  features: {
    dashboard: true,
    kundali: true,
    compatibility: true,
    panchang: true,
    dasha: true,
  },
};
