export const apiEndpoints = {
  auth: 'auth',
  user: 'user',
  kundali: {
    root: 'Kundali',
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
  muhurtha: 'Muhurtha',
  learning: {
    levels: 'learning/levels',
    level: 'learning/levels/:levelCode',
    session: 'learning/sessions/:sessionGuid',
  },
  reportShare: {
    sendEmail: 'ReportShare/sendEmail',
    pdf: 'ReportShare/pdf',
  },
  gochar: {
    root: 'gochar',
    currentLifeFocus: 'gochar/current-life-focus',
  },
  prediction: {
    question: 'Prediction/question',
  },
  numerology: {
    calculate: 'numerology/calculate',
  },
} as const;
