import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';
import { DashboardComponent } from './modules/dashboard/dashboard.component';
import { KundaliComponent } from './modules/kundali/kundali.component';
import { CompatibilityComponent } from './modules/compatibility/compatibility.component';
import { PanchangComponent } from './modules/panchang/panchang.component';
import { MuhurthaComponent } from './modules/panchang/muhurtha.component';
import { GrahaPraveshaMuhurthaComponent } from './modules/panchang/graha-pravesha-muhurtha.component';
import { MahadashaPageComponent } from './features/mahadasha/pages/mahadasha-page/mahadasha-page.component';
import { KundaliLandingComponent } from './modules/home/kundali-landing/kundali-landing.component';
import { adminGuard, authGuard } from './core/auth/auth.guard';
import { preferredLanguageGuard } from './core/auth/preferred-language.guard';
import { WhiteLabelSettingsComponent } from './modules/white-label-settings/white-label-settings.component';
import { ChangePasswordComponent } from './modules/account/change-password/change-password.component';
import { ProfileComponent } from './modules/account/profile/profile.component';
import { UserLoginControlComponent } from './modules/user-login-control/user-login-control.component';
import { AuditLogsComponent } from './modules/audit-logs/audit-logs.component';
import { FeedbackInboxComponent } from './modules/feedback-inbox/feedback-inbox.component';
import { PlanOffersComponent } from './modules/plan-offers/plan-offers.component';
import { UserAnalysisComponent } from './modules/user-analysis/user-analysis.component';
import { PortalNotesPageComponent } from './modules/portal-notes/portal-notes-page.component';
import { LifeAreaAnalysisComponent } from './modules/life-area-analysis/life-area-analysis.component';
import { unsavedChangesGuard } from './core/guards/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: '',
    component: KundaliLandingComponent,
    pathMatch: 'full',
    data: { title: 'KundaliScore' },
  },
  {
  path: 'login',
  loadComponent: () =>
    import('./modules/auth/login/login.component')
      .then(m => m.LoginComponent)
},
  {
  path: 'register',
  loadComponent: () =>
    import('./modules/auth/register/register.component')
      .then(m => m.RegisterComponent)
},
  {
  path: 'select-language',
  canActivate: [authGuard],
  loadComponent: () =>
    import('./modules/auth/preferred-language/preferred-language.component')
      .then(m => m.PreferredLanguageComponent)
},
  {
    path: 'pricing',
    loadComponent: () =>
      import('./modules/pricing/pricing.component')
        .then(m => m.PricingComponent),
    data: { title: 'Plans' },
  },
  ...['about', 'services', 'contact', 'terms', 'privacy', 'refund-policy', 'shipping-policy', 'support', 'grievance'].map((page) => ({
    path: page,
    loadComponent: () =>
      import('./modules/company/company-page.component')
        .then(m => m.CompanyPageComponent),
    data: { page, title: page },
  })),
  {
    path: 'horaverse',
    loadComponent: () =>
      import('./modules/horaverse/horaverse-page.component')
        .then(m => m.HoraversePageComponent),
    data: { title: 'HoraVerse' },
  },
  {
    path: 'app',
    component: LayoutComponent,
    canActivate: [authGuard, preferredLanguageGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        component: DashboardComponent,
        data: { title: 'Dashboard' },
      },
      {
        path: 'kundali/saved/:id',
        component: KundaliComponent,
        canDeactivate: [unsavedChangesGuard],
        data: { title: 'Saved Kundali' },
      },
      {
        path: 'kundali',
        component: KundaliComponent,
        canDeactivate: [unsavedChangesGuard],
        data: { title: 'Generate Kundali' },
      },
      {
        path: 'compatibility/saved/:id',
        component: CompatibilityComponent,
        canDeactivate: [unsavedChangesGuard],
        data: { title: 'Saved Compatibility' },
      },
      {
        path: 'compatibility',
        component: CompatibilityComponent,
        canDeactivate: [unsavedChangesGuard],
        data: { title: 'Compatibility Analysis' },
      },
      {
        path: 'panchang',
        component: PanchangComponent,
        data: { title: 'Panchang' },
      },
      {
        path: 'panchang/muhurtha',
        component: MuhurthaComponent,
        data: { title: 'Muhurtha Finder' },
      },
      {
        path: 'panchang/muhurtha/graha-pravesha',
        component: GrahaPraveshaMuhurthaComponent,
        data: { title: 'Graha Pravesha Muhurtha' },
      },
      {
        path: 'learning',
        loadComponent: () =>
          import('./modules/learning/learning-home.component')
            .then(m => m.LearningHomeComponent),
        data: { title: 'Jyotish Study' },
      },
      {
        path: 'learning/level/:levelCode',
        loadComponent: () =>
          import('./modules/learning/learning-home.component')
            .then(m => m.LearningHomeComponent),
        data: { title: 'Jyotish Study' },
      },
      {
        path: 'learning/session/:sessionGuid',
        loadComponent: () =>
          import('./modules/learning/learning-session.component')
            .then(m => m.LearningSessionComponent),
        data: { title: 'Jyotish Study Session' },
      },
      {
        path: 'dasha',
        component: MahadashaPageComponent,
        data: { title: 'Dasha Analysis' },
      },
      {
        path: 'life-area-analysis',
        component: LifeAreaAnalysisComponent,
        data: { title: 'Life area analysis' },
      },
      {
        path: 'notes',
        component: PortalNotesPageComponent,
        data: { title: 'Notes' },
      },
      {
        path: 'numerology',
        loadComponent: () =>
          import('./modules/numerology/numerology.component')
            .then(m => m.NumerologyComponent),
        data: { title: 'Numerology' },
      },
      {
        path: 'mahadasha',
        component: MahadashaPageComponent,
        data: { title: 'Maha Dasha Analysis' },
      },
      {
        path: 'billing',
        loadComponent: () =>
          import('./modules/pricing/pricing.component')
            .then(m => m.PricingComponent),
        data: { title: 'Plans' },
      },
      {
        path: 'white-label-settings',
        component: WhiteLabelSettingsComponent,
        data: { title: 'White-label Settings' },
      },
      {
        path: 'user-login-control',
        component: UserLoginControlComponent,
        canActivate: [adminGuard],
        data: { title: 'User Login Control' },
      },
      {
        path: 'audit-logs',
        component: AuditLogsComponent,
        canActivate: [adminGuard],
        data: { title: 'Audit Logs' },
      },
      {
        path: 'feedback',
        component: FeedbackInboxComponent,
        canActivate: [adminGuard],
        data: { title: 'Feedback' },
      },
      {
        path: 'plan-offers',
        component: PlanOffersComponent,
        canActivate: [adminGuard],
        data: { title: 'Plans and offers' },
      },
      {
        path: 'user-analysis',
        component: UserAnalysisComponent,
        canActivate: [adminGuard],
        data: { title: 'User analysis' },
      },
      {
        path: 'change-password',
        component: ChangePasswordComponent,
        data: { title: 'Change Password' },
      },
      {
        path: 'practice',
        redirectTo: 'profile',
        pathMatch: 'full',
      },
      {
        path: 'profile',
        component: ProfileComponent,
        data: { title: 'My Profile' },
      },
    ],
  },
  {
    path: 'dashboard',
    redirectTo: 'app/dashboard',
    pathMatch: 'full',
  },
  {
    path: 'app/kundali',
  canActivate: [authGuard],
  loadComponent: () =>
    import('./modules/kundali/kundali.component')
      .then(m => m.KundaliComponent)
  },
  {
    path: 'compatibility',
    redirectTo: 'app/compatibility',
    pathMatch: 'full',
  },
  {
    path: 'panchang',
    redirectTo: 'app/panchang',
    pathMatch: 'full',
  },
  {
    path: 'muhurtha',
    redirectTo: 'app/panchang/muhurtha',
    pathMatch: 'full',
  },
  {
    path: 'muhurta/graha-pravesha',
    redirectTo: 'app/panchang/muhurtha/graha-pravesha',
    pathMatch: 'full',
  },
  {
    path: 'learning',
    redirectTo: 'app/learning',
    pathMatch: 'full',
  },
  {
    path: 'learning/level/:levelCode',
    redirectTo: 'app/learning/level/:levelCode',
    pathMatch: 'full',
  },
  {
    path: 'learning/session/:sessionGuid',
    redirectTo: 'app/learning/session/:sessionGuid',
    pathMatch: 'full',
  },
  {
    path: 'dasha',
    redirectTo: 'app/dasha',
    pathMatch: 'full',
  },
  {
    path: 'mahadasha',
    redirectTo: 'app/mahadasha',
    pathMatch: 'full',
  },
  {
    path: 'notes',
    redirectTo: 'app/notes',
    pathMatch: 'full',
  },
  {
    path: 'numerology',
    redirectTo: 'app/numerology',
    pathMatch: 'full',
  },
  {
    path: 'white-label-settings',
    redirectTo: 'app/white-label-settings',
    pathMatch: 'full',
  },
  {
    path: 'change-password',
    redirectTo: 'app/change-password',
    pathMatch: 'full',
  },
  {
    path: 'profile',
    redirectTo: 'app/profile',
    pathMatch: 'full',
  },
  {
    path: 'user-login-control',
    redirectTo: 'app/user-login-control',
    pathMatch: 'full',
  },
  {
    path: 'audit-logs',
    redirectTo: 'app/audit-logs',
    pathMatch: 'full',
  },
  {
    path: 'feedback',
    redirectTo: 'app/feedback',
    pathMatch: 'full',
  },
  {
    path: 'plan-offers',
    redirectTo: 'app/plan-offers',
    pathMatch: 'full',
  },
  {
    path: 'life-area-analysis',
    redirectTo: 'app/life-area-analysis',
    pathMatch: 'full',
  },
  {
    path: 'user-analysis',
    redirectTo: 'app/user-analysis',
    pathMatch: 'full',
  },
  {
    path: 'home',
    redirectTo: '',
    pathMatch: 'full',
  },

  {
    path: '**',
    redirectTo: '',
  },
];












