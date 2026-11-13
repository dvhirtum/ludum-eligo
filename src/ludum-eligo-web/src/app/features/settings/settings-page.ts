import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ImportJob, SettingsApi } from './settings-api';

export const IMPORT_POLL_INTERVAL_MS = 3000;
const BGG_UNAVAILABLE = 'BoardGameGeek is unavailable. Try again later.';

/** Links a BGG username and imports the BGG collection, showing the import status. */
@Component({
  selector: 'app-settings-page',
  imports: [MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>Settings</h1>

    <mat-card appearance="outlined">
      <mat-card-header>
        <mat-card-title>BoardGameGeek username</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <p>Link your BGG username to import the games you own on BGG. No BGG password is needed.</p>
        <form (submit)="$event.preventDefault(); save()">
          <mat-form-field appearance="outline" class="username">
            <mat-label>BGG username</mat-label>
            <input matInput name="bggUsername" autocomplete="off" [value]="username()" (input)="username.set($any($event.target).value)" />
          </mat-form-field>
          <button mat-flat-button type="submit" [disabled]="saving()">Save</button>
        </form>
        @if (profileMessage(); as text) {
          <p class="message" role="status">{{ text }}</p>
        }
        @if (profileError(); as text) {
          <p class="error" role="alert">{{ text }}</p>
        }
      </mat-card-content>
    </mat-card>

    <mat-card appearance="outlined">
      <mat-card-header>
        <mat-card-title>Import from BGG</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <p>Adds the base games you own on BGG that are not in your collection yet. Nothing is removed or changed, and nothing is written to BGG.</p>
        @if (!linkedUsername()) {
          <p>Link your BGG username to import your collection.</p>
        }
        @if (job(); as current) {
          <div data-testid="import-status" class="status" [class.error]="current.status === 'Failed'">
            @switch (current.status) {
              @case ('Queued') {
                <p>Import queued…</p>
                <mat-progress-bar mode="indeterminate" />
              }
              @case ('Running') {
                <p>Importing… BGG can take a few minutes to prepare a collection.</p>
                <mat-progress-bar mode="indeterminate" />
              }
              @case ('Succeeded') {
                <p>
                  Import finished: {{ current.added }} {{ current.added === 1 ? 'game' : 'games' }} added,
                  {{ current.alreadyPresent }} already in your collection.
                </p>
              }
              @case ('Failed') {
                <p>Import failed: {{ current.error }}</p>
              }
            }
          </div>
        }
        @if (importError(); as text) {
          <p class="error" role="alert">{{ text }}</p>
        }
      </mat-card-content>
      <mat-card-actions>
        <button mat-flat-button type="button" [disabled]="!canImport()" (click)="startImport()">Import from BGG</button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: `
    mat-card {
      margin-bottom: 16px;
    }
    form {
      display: flex;
      align-items: baseline;
      gap: 8px;
      flex-wrap: wrap;
    }
    .username {
      flex: 1 1 240px;
    }
    .message {
      color: var(--mat-sys-primary);
    }
    .error {
      color: var(--mat-sys-error);
    }
  `,
})
export class SettingsPage {
  private readonly api = inject(SettingsApi);
  private pollTimer: ReturnType<typeof setTimeout> | undefined;

  protected readonly username = signal('');
  protected readonly linkedUsername = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly profileMessage = signal<string | null>(null);
  protected readonly profileError = signal<string | null>(null);

  protected readonly job = signal<ImportJob | null>(null);
  protected readonly starting = signal(false);
  protected readonly importError = signal<string | null>(null);

  private readonly importActive = computed(() => {
    const status = this.job()?.status;
    return status === 'Queued' || status === 'Running';
  });

  protected readonly canImport = computed(
    () => this.linkedUsername() !== null && !this.importActive() && !this.starting(),
  );

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.pollTimer));
    void this.loadProfile();
    void this.loadLatestImport();
  }

  protected async save(): Promise<void> {
    const value = this.username().trim() || null;
    this.saving.set(true);
    this.profileMessage.set(null);
    this.profileError.set(null);
    try {
      const profile = await this.api.setBggUsername(value);
      this.linkedUsername.set(profile.bggUsername);
      this.username.set(profile.bggUsername ?? '');
      this.profileMessage.set(profile.bggUsername ? 'BGG username saved.' : 'BGG username removed.');
    } catch (error) {
      this.profileError.set(usernameError(error));
    } finally {
      this.saving.set(false);
    }
  }

  protected async startImport(): Promise<void> {
    this.starting.set(true);
    this.importError.set(null);
    try {
      this.showJob(await this.api.startImport());
    } catch (error) {
      this.importError.set(
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict
          ? 'Link your BGG username before importing.'
          : 'The import could not be started. Please try again.',
      );
    } finally {
      this.starting.set(false);
    }
  }

  private async loadProfile(): Promise<void> {
    try {
      const profile = await this.api.getProfile();
      this.linkedUsername.set(profile.bggUsername);
      this.username.set(profile.bggUsername ?? '');
    } catch {
      this.profileError.set('Could not load your settings. Please reload the page.');
    }
  }

  private async loadLatestImport(): Promise<void> {
    try {
      this.showJob(await this.api.latestImport());
    } catch {
      this.importError.set('Could not load the import status.');
      this.schedulePoll();
    }
  }

  /** Shows the job and keeps polling while it is queued or running. */
  private showJob(job: ImportJob | null): void {
    this.job.set(job);
    if (this.importActive()) {
      this.schedulePoll();
    }
  }

  private schedulePoll(): void {
    clearTimeout(this.pollTimer);
    this.pollTimer = setTimeout(() => void this.loadLatestImport(), IMPORT_POLL_INTERVAL_MS);
  }
}

function usernameError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === HttpStatusCode.ServiceUnavailable) {
      return BGG_UNAVAILABLE;
    }
    const message = (error.error as { errors?: Record<string, string[]> } | null)?.errors?.['bggUsername']?.[0];
    if (error.status === HttpStatusCode.BadRequest && message) {
      return message;
    }
  }
  return 'The username could not be saved. Please try again.';
}
