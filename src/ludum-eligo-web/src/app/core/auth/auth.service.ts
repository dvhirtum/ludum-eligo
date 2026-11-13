import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BrowserNavigation } from './browser-navigation';

export interface CurrentUser {
  displayName: string;
}

/** Who is signed in. The session itself lives in an HttpOnly cookie the SPA never sees. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly navigation = inject(BrowserNavigation);
  private readonly currentUser = signal<CurrentUser | null>(null);

  readonly user = this.currentUser.asReadonly();
  readonly isSignedIn = computed(() => this.currentUser() !== null);

  /** Asks the API who is signed in. A 401 (or any error) means signed out. */
  async load(): Promise<void> {
    try {
      this.currentUser.set(await firstValueFrom(this.http.get<CurrentUser>('/api/auth/me')));
    } catch {
      this.currentUser.set(null);
    }
  }

  /** Leaves the SPA for the Google sign-in flow; the API returns the browser to `returnUrl`. */
  signIn(returnUrl: string): void {
    this.navigation.assign(`/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  async signOut(): Promise<void> {
    await firstValueFrom(this.http.post<void>('/api/auth/logout', null));
    this.currentUser.set(null);
  }

  markSignedOut(): void {
    this.currentUser.set(null);
  }
}
