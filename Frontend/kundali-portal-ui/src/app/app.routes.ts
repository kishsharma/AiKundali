import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';
import { DashboardComponent } from './modules/dashboard/dashboard.component';
import { KundaliComponent } from './modules/kundali/kundali.component';
import { CompatibilityComponent } from './modules/compatibility/compatibility.component';
import { PanchangComponent } from './modules/panchang/panchang.component';
import { MahadashaPageComponent } from './features/mahadasha/pages/mahadasha-page/mahadasha-page.component';
import { KundaliLandingComponent } from './modules/home/kundali-landing/kundali-landing.component';
import { authGuard } from './core/auth/auth.guard';
import { WhiteLabelSettingsComponent } from './modules/white-label-settings/white-label-settings.component';
import { ChangePasswordComponent } from './modules/account/change-password/change-password.component';

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
    path: 'app',
    component: LayoutComponent,
    canActivate: [authGuard],
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
        path: 'kundali',
        component: KundaliComponent,
        data: { title: 'Generate Kundali' },
      },
      {
        path: 'compatibility',
        component: CompatibilityComponent,
        data: { title: 'Compatibility Analysis' },
      },
      {
        path: 'panchang',
        component: PanchangComponent,
        data: { title: 'Panchang' },
      },
      {
        path: 'dasha',
        component: MahadashaPageComponent,
        data: { title: 'Dasha Analysis' },
      },
      {
        path: 'mahadasha',
        component: MahadashaPageComponent,
        data: { title: 'Maha Dasha Analysis' },
      },
      {
        path: 'white-label-settings',
        component: WhiteLabelSettingsComponent,
        data: { title: 'White-label Settings' },
      },
      {
        path: 'change-password',
        component: ChangePasswordComponent,
        data: { title: 'Change Password' },
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
    path: 'home',
    redirectTo: '',
    pathMatch: 'full',
  },

  {
    path: '**',
    redirectTo: '',
  },
];
