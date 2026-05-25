// Theme configuration - colors, typography, spacing

export const themeConfig = {
  colors: {
    // Primary Astrology Palette
    primary: {
      light: '#9C4EE0',
      main: '#7E3BA8',
      dark: '#6A2E8F',
    },
    secondary: {
      light: '#FF6B9D',
      main: '#FF1E64',
      dark: '#CC1850',
    },
    accent: {
      light: '#00BCD4',
      main: '#0097A7',
      dark: '#00838F',
    },
    // Neutral
    neutral: {
      white: '#FFFFFF',
      light: '#F5F5F5',
      lighter: '#FAFAFA',
      gray100: '#F0F0F0',
      gray200: '#E0E0E0',
      gray300: '#BDBDBD',
      gray400: '#9E9E9E',
      gray500: '#757575',
      gray600: '#616161',
      gray700: '#424242',
      dark: '#212121',
      darker: '#121212',
    },
    // Status Colors
    success: '#4CAF50',
    warning: '#FF9800',
    error: '#F44336',
    info: '#2196F3',
  },
  typography: {
    fontFamily: "'Inter', 'Segoe UI', sans-serif",
    fontSize: {
      xs: '0.75rem',
      sm: '0.875rem',
      base: '1rem',
      lg: '1.125rem',
      xl: '1.25rem',
      '2xl': '1.5rem',
      '3xl': '1.875rem',
      '4xl': '2.25rem',
    },
    fontWeight: {
      light: 300,
      normal: 400,
      medium: 500,
      semibold: 600,
      bold: 700,
    },
  },
  spacing: {
    xs: '0.25rem',
    sm: '0.5rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2rem',
    '2xl': '3rem',
    '3xl': '4rem',
  },
  borderRadius: {
    none: '0',
    sm: '0.25rem',
    md: '0.5rem',
    lg: '1rem',
    xl: '1.5rem',
    full: '9999px',
  },
  shadows: {
    sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
    md: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
    lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1)',
    xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1)',
  },
  breakpoints: {
    xs: '460px',
    sm: '640px',
    md: '768px',
    lg: '1024px',
    xl: '1280px',
    '2xl': '1536px',
  },
};
