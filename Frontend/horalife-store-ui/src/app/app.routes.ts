import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { adminGuard } from './core/auth/admin.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.page').then((m) => m.LoginPage)
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register.page').then((m) => m.RegisterPage)
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/catalog/catalog.page').then((m) => m.CatalogPage)
      },
      {
        path: 'products/:slug',
        loadComponent: () => import('./pages/product/product.page').then((m) => m.ProductPage)
      },
      {
        path: 'cart',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/cart/cart.page').then((m) => m.CartPage)
      },
      {
        path: 'checkout',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/checkout/checkout.page').then((m) => m.CheckoutPage)
      },
      {
        path: 'order-received',
        canActivate: [authGuard],
        loadComponent: () => import('./pages/order-received/order-received.page').then((m) => m.OrderReceivedPage)
      },
      {
        path: 'admin/inventory',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./pages/admin-inventory/admin-inventory.page').then((m) => m.AdminInventoryPage)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
