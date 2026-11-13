import { Routes } from '@angular/router';
import { signedInGuard, signedOutGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [signedOutGuard],
    loadComponent: () => import('./features/landing/landing-page').then((m) => m.LandingPage),
  },
  {
    path: 'collection',
    canActivate: [signedInGuard],
    loadComponent: () => import('./features/collection/collection-page').then((m) => m.CollectionPage),
  },
  { path: '**', redirectTo: '' },
];
