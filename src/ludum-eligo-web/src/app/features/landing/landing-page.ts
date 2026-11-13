import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-landing-page',
  imports: [MatButtonModule, MatCardModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-card appearance="outlined">
      <mat-card-header>
        <mat-card-title>Pick your next board game</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <p>Keep track of the games you own, then let Ludum Eligo help your group choose what to play.</p>
        @if (signInFailed) {
          <p class="error" role="alert">Sign-in did not complete. Please try again.</p>
        }
      </mat-card-content>
      <mat-card-actions>
        <button mat-flat-button type="button" (click)="signIn()">Sign in with Google</button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: `
    mat-card {
      max-width: 480px;
      margin: 48px auto;
    }
    .error {
      color: var(--mat-sys-error);
    }
  `,
})
export class LandingPage {
  private readonly auth = inject(AuthService);
  protected readonly signInFailed =
    inject(ActivatedRoute).snapshot.queryParamMap.get('signInFailed') === 'true';

  protected signIn(): void {
    this.auth.signIn('/collection');
  }
}
