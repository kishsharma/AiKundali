import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';
import { DashboardComponent } from './modules/dashboard/dashboard.component';
import { KundaliComponent } from './modules/kundali/kundali.component';
import { CompatibilityComponent } from './modules/compatibility/compatibility.component';
import { PanchangComponent } from './modules/panchang/panchang.component';
import { MahadashaPageComponent } from './features/mahadasha/pages/mahadasha-page/mahadasha-page.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
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
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
