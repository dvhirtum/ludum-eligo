import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** A 401 from the API means the session ended: switch to signed out and show the landing page. */
export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return next(request).pipe(
    catchError((error: unknown) => {
      // AuthService.load() handles a 401 from the current-user check itself, at startup.
      if (
        error instanceof HttpErrorResponse &&
        error.status === HttpStatusCode.Unauthorized &&
        request.url !== '/api/auth/me'
      ) {
        auth.markSignedOut();
        void router.navigateByUrl('/');
      }
      return throwError(() => error);
    }),
  );
};
