import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { computed, signal } from '@angular/core';
import { vi } from 'vitest';
import { App } from './app';
import { AuthService } from './core/auth/auth.service';

describe('App', () => {
  const user = signal<{ displayName: string } | null>(null);
  const auth = {
    user,
    isSignedIn: computed(() => user() !== null),
    signOut: vi.fn().mockResolvedValue(undefined),
  };

  async function render() {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture;
  }

  beforeEach(() => auth.signOut.mockClear());

  it('should show the application name in the toolbar', async () => {
    const fixture = await render();
    expect((fixture.nativeElement as HTMLElement).querySelector('mat-toolbar')?.textContent).toContain('Ludum Eligo');
  });

  it('should not offer sign-out when nobody is signed in', async () => {
    user.set(null);
    const fixture = await render();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Sign out');
  });

  it('should show the display name and sign out to the landing page when signed in', async () => {
    user.set({ displayName: 'Ada Lovelace' });
    const fixture = await render();
    const navigate = vi.fn<Router['navigateByUrl']>().mockResolvedValue(true);
    TestBed.inject(Router).navigateByUrl = navigate;
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('Ada Lovelace');
    [...page.querySelectorAll('button')].find((b) => b.textContent?.includes('Sign out'))!.click();
    await fixture.whenStable();

    expect(auth.signOut).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/');
  });
});
