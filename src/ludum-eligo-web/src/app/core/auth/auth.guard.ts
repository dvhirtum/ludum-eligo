import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Signed-in routes: anonymous visitors go to the landing page. */
export const signedInGuard: CanActivateFn = () =>
  inject(AuthService).isSignedIn() || inject(Router).parseUrl('/');

/** The landing page: signed-in users go straight to their collection. */
export const signedOutGuard: CanActivateFn = () =>
  !inject(AuthService).isSignedIn() || inject(Router).parseUrl('/collection');
