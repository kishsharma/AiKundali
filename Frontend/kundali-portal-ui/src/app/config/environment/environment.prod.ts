// Environment configuration - Production

export const environment = {
  production: true,
  apiBaseUrl: 'https://api.kundali-portal.com/api',
  apiTimeout: 30000,
  features: {
    dashboard: true,
    kundali: true,
    compatibility: true,
    panchang: true,
    dasha: true,
  },
};
