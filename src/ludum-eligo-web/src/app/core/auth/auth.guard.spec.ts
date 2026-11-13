import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { AuthService } from './auth.service';
import { signedInGuard, signedOutGuard } from './auth.guard';

describe('auth guards', () => {
  const signedIn = signal(false);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: { isSignedIn: signedIn } }],
    });
  });

  const run = (guard: CanActivateFn) =>
    TestBed.runInInjectionContext(() => guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));

  it('should allow a signed-in user onto a signed-in route', () => {
    signedIn.set(true);
    expect(run(signedInGuard)).toBe(true);
  });

  it('should send an anonymous visitor from a signed-in route to the landing page', () => {
    signedIn.set(false);
    const result = run(signedInGuard);
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/');
  });

  it('should send a signed-in user from the landing page to the collection', () => {
    signedIn.set(true);
    expect((run(signedOutGuard) as UrlTree).toString()).toBe('/collection');
  });

  it('should allow an anonymous visitor onto the landing page', () => {
    signedIn.set(false);
    expect(run(signedOutGuard)).toBe(true);
  });
});
