// Environment configuration - Staging

export const environment = {
  production: false,
  apiBaseUrl: 'https://staging-api.kundali-portal.com/api',
  apiTimeout: 30000,
  features: {
    dashboard: true,
    kundali: true,
    compatibility: true,
    panchang: true,
    dasha: true,
  },
};
