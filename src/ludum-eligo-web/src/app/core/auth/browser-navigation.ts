import { Injectable } from '@angular/core';

/** Full-page browser navigation, for leaving the SPA (the Google sign-in flow). */
@Injectable({ providedIn: 'root' })
export class BrowserNavigation {
  assign(url: string): void {
    window.location.assign(url);
  }
}
