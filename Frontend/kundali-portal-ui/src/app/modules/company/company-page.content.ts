import { business } from '../../config/business.config';

export interface CompanySection {
  heading: string;
  paragraphs: string[];
}

export interface CompanyPageContent {
  title: string;
  intro: string[];
  sections: CompanySection[];
  showLegalIds: boolean;
  showContact: boolean;
}

const relationship = business.relationship;

export const companyPages: Record<string, CompanyPageContent> = {
  about: {
    title: 'About HoraLife',
    intro: [
      relationship,
      `${business.brandName} is a Vedic astrology product on ${business.domain}. Charts, compatibility, Dhruva, and panchang are prepared inside ${business.brandName}. ${business.legalName} is the legal operator of that service.`,
    ],
    sections: [],
    showLegalIds: true,
    showContact: false,
  },
  services: {
    title: 'Astrology Services',
    intro: [
      relationship,
      `${business.brandName} currently offers these tools on ${business.domain}.`,
    ],
    sections: [
      {
        heading: 'Kundali',
        paragraphs: ['Birth charts, planetary positions, dasha periods, and downloadable reports for a person whose birth details you enter.'],
      },
      {
        heading: 'Compatibility',
        paragraphs: ['A match reading for two people, using the birth details you provide.'],
      },
      {
        heading: 'Dhruva',
        paragraphs: ['Questions about a chart you already opened in HoraLife. Free accounts include a daily limit. Premium and Premium Plus include AI mode.'],
      },
      {
        heading: 'Panchang',
        paragraphs: ['Daily panchang and muhurtha timings for a place and date you choose.'],
      },
    ],
    showLegalIds: false,
    showContact: false,
  },
  contact: {
    title: 'Contact Us',
    intro: [
      relationship,
      `Write to ${business.brandName} about your account, a chart, or a payment. The support email and phone below are not yet updated.`,
    ],
    sections: [],
    showLegalIds: false,
    showContact: true,
  },
  terms: {
    title: 'Terms & Conditions',
    intro: [
      relationship,
      `These terms cover use of ${business.brandName} at ${business.domain}. ${business.legalName} operates the service.`,
    ],
    sections: [
      {
        heading: 'Using HoraLife',
        paragraphs: [
          'Create an account with accurate details. You are responsible for the birth data you enter and for keeping your login private.',
          'Chart readings are interpretive guidance. They are not a promise of a result in health, law, finance, or any other matter.',
        ],
      },
      {
        heading: 'Plans and payment',
        paragraphs: [
          `Paid plans are billed through Razorpay, the payment processor for ${business.brandName}. ${business.legalName} is the business that receives that payment.`,
          'Plan names, prices, and included limits are shown on the Plans page and in checkout. A paid plan can be cancelled from the account before the next cycle when a subscription is active.',
        ],
      },
      {
        heading: 'Acceptable use',
        paragraphs: [
          'Do not misuse the service, attempt to access another person’s account, or present a HoraLife report as issued by someone else.',
        ],
      },
    ],
    showLegalIds: true,
    showContact: true,
  },
  privacy: {
    title: 'Privacy Policy',
    intro: [
      relationship,
      `${business.legalName} is responsible for personal information collected through ${business.brandName}.`,
    ],
    sections: [
      {
        heading: 'What HoraLife stores',
        paragraphs: [
          'Account details such as your name, email, and phone, plus birth details you enter for a chart, compatibility match, or panchang place.',
          'Payment status comes back from Razorpay. HoraLife does not ask you to type a card number into the site. Checkout runs on Razorpay.',
        ],
      },
      {
        heading: 'How it is used',
        paragraphs: [
          'The details are used to sign you in, calculate the chart or match you requested, apply your plan, and respond to a support request.',
          'A specific retention period and grievance contact will be published here once the registered business details are updated.',
        ],
      },
    ],
    showLegalIds: true,
    showContact: true,
  },
  'refund-policy': {
    title: 'Refund & Cancellation Policy',
    intro: [
      relationship,
      `Payments for ${business.brandName} plans are processed by Razorpay for the business operated by ${business.legalName}.`,
    ],
    sections: [
      {
        heading: 'Cancellation',
        paragraphs: [
          'A paid plan with an active subscription can be cancelled from the account. Cancellation is sent to Razorpay and applies before the next cycle.',
        ],
      },
      {
        heading: 'Refunds',
        paragraphs: [
          'A refund window and the address for a refund request will be published here once the registered business details are updated.',
          'When a refund is issued, it is returned through Razorpay to the original payment method.',
        ],
      },
    ],
    showLegalIds: true,
    showContact: true,
  },
  'shipping-policy': {
    title: 'Shipping / Delivery Policy',
    intro: [
      relationship,
      `${business.brandName} delivers digital astrology tools and reports inside ${business.domain}. There is no physical shipment and no shipping charge.`,
    ],
    sections: [
      {
        heading: 'Delivery',
        paragraphs: [
          'A chart, compatibility reading, Dhruva answer, panchang, or downloaded report is available in your HoraLife account after the request succeeds.',
          'A paid plan is applied to the account after Razorpay confirms the payment.',
        ],
      },
    ],
    showLegalIds: true,
    showContact: false,
  },
  support: {
    title: 'Help & Support',
    intro: [
      relationship,
      `For help with a chart, a login, or a plan, contact ${business.brandName}. The email and phone below are not yet updated.`,
    ],
    sections: [
      {
        heading: 'What to include',
        paragraphs: [
          'Your account email and a short description of the page you were on. For a payment question, include the time of the attempt. Do not send card numbers.',
        ],
      },
    ],
    showLegalIds: false,
    showContact: true,
  },
  grievance: {
    title: 'Grievance Redressal',
    intro: [
      relationship,
      `A grievance about ${business.brandName} is handled by ${business.legalName}. The grievance email, phone, and officer details are not yet updated.`,
    ],
    sections: [
      {
        heading: 'How to raise a grievance',
        paragraphs: [
          'Send the account email, the page or report involved, and what you want corrected. The registered address and the name of the grievance officer will be added here after LLP registration details are available.',
        ],
      },
    ],
    showLegalIds: true,
    showContact: true,
  },
};
