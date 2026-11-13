import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { vi } from 'vitest';
import { AuthService } from './auth.service';
import { BrowserNavigation } from './browser-navigation';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  const navigation = { assign: vi.fn() };

  beforeEach(() => {
    navigation.assign.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BrowserNavigation, useValue: navigation },
      ],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should hold the display name when loading while signed in', async () => {
    const loading = service.load();
    http.expectOne('/api/auth/me').flush({ displayName: 'Ada Lovelace' });
    await loading;

    expect(service.user()).toEqual({ displayName: 'Ada Lovelace' });
    expect(service.isSignedIn()).toBe(true);
  });

  it('should be signed out when loading returns 401', async () => {
    const loading = service.load();
    http.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    await loading;

    expect(service.user()).toBeNull();
    expect(service.isSignedIn()).toBe(false);
  });

  it('should navigate the browser to the login endpoint with the return url when signing in', () => {
    service.signIn('/collection');

    expect(navigation.assign).toHaveBeenCalledWith('/api/auth/login?returnUrl=%2Fcollection');
  });

  it('should post to logout and clear the user when signing out', async () => {
    const loading = service.load();
    http.expectOne('/api/auth/me').flush({ displayName: 'Ada Lovelace' });
    await loading;

    const signingOut = service.signOut();
    const request = http.expectOne('/api/auth/logout');
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await signingOut;

    expect(service.isSignedIn()).toBe(false);
  });
});
