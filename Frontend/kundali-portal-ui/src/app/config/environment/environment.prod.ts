// Environment configuration - Production

export const environment = {
  production: true,
  assetVersion: '0.8.1-outlook',
  apiBaseUrl: 'https://api.kundali-portal.com/api',
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
