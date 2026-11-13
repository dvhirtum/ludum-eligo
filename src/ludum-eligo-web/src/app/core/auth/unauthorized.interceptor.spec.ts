import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from './auth.service';
import { unauthorizedInterceptor } from './unauthorized.interceptor';

describe('unauthorizedInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let navigate: ReturnType<typeof vi.fn<Router['navigateByUrl']>>;
  const auth = { markSignedOut: vi.fn() };

  beforeEach(() => {
    auth.markSignedOut.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([unauthorizedInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    navigate = vi.fn<Router['navigateByUrl']>().mockResolvedValue(true);
    TestBed.inject(Router).navigateByUrl = navigate;
  });

  it('should mark the user signed out and go to the landing page when the api returns 401', async () => {
    const request = firstValueFrom(http.get('/api/collection'));
    controller.expectOne('/api/collection').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(request).rejects.toBeTruthy();
    expect(auth.markSignedOut).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('should leave the current-user check to the auth service when it returns 401', async () => {
    const request = firstValueFrom(http.get('/api/auth/me'));
    controller.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(request).rejects.toBeTruthy();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should leave other errors alone', async () => {
    const request = firstValueFrom(http.get('/api/collection'));
    controller.expectOne('/api/collection').flush(null, { status: 500, statusText: 'Server Error' });

    await expect(request).rejects.toBeTruthy();
    expect(auth.markSignedOut).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });
});
