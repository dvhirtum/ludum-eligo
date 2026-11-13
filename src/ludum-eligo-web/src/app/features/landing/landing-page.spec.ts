import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { vi } from 'vitest';
import { AuthService } from '../../core/auth/auth.service';
import { LandingPage } from './landing-page';

describe('LandingPage', () => {
  const auth = { signIn: vi.fn() };

  async function render(queryParams: Record<string, string> = {}) {
    await TestBed.configureTestingModule({
      imports: [LandingPage],
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(LandingPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => auth.signIn.mockReset());

  it('should sign in with google and return to the collection when the button is clicked', async () => {
    const page = await render();

    [...page.querySelectorAll('button')].find((b) => b.textContent?.includes('Sign in with Google'))!.click();

    expect(auth.signIn).toHaveBeenCalledWith('/collection');
  });

  it('should show that sign-in did not complete when google sign-in failed', async () => {
    const page = await render({ signInFailed: 'true' });

    expect(page.textContent).toContain('Sign-in did not complete');
  });

  it('should not show a failure message on a normal visit', async () => {
    const page = await render();

    expect(page.textContent).not.toContain('Sign-in did not complete');
  });
});
