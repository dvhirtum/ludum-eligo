import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { CollectionApi, GameSearchResult } from './collection-api';

export const SEARCH_DEBOUNCE_MS = 300;
export const MIN_SEARCH_LENGTH = 3;
const BGG_UNAVAILABLE = 'BoardGameGeek is unavailable. Try again later.';

/** Searches BGG (debounced) and adds a result to the collection. */
@Component({
  selector: 'app-add-game-page',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="heading">
      <h1>Add a game</h1>
      <a mat-button routerLink="/collection">Back to collection</a>
    </header>

    <mat-form-field class="search" appearance="outline">
      <mat-label>Search BoardGameGeek</mat-label>
      <input matInput name="search" autocomplete="off" (input)="onInput($any($event.target).value)" />
      @if (tooShort()) {
        <mat-hint>Type at least {{ minLength }} characters.</mat-hint>
      }
    </mat-form-field>

    @if (message(); as text) {
      <p class="message" role="status">{{ text }}</p>
    }
    @if (error(); as text) {
      <p class="error" role="alert">{{ text }}</p>
    }

    @if (searching()) {
      <mat-progress-bar mode="indeterminate" />
    } @else if (results(); as found) {
      <ul class="results">
        @for (result of found; track result.bggId) {
          <li class="result" data-testid="result">
            <span class="name">
              {{ result.name }}
              @if (result.yearPublished) {
                ({{ result.yearPublished }})
              }
            </span>
            <button
              mat-stroked-button
              type="button"
              [attr.aria-label]="'Add ' + result.name"
              [disabled]="adding() === result.bggId"
              (click)="add(result)"
            >
              Add
            </button>
          </li>
        } @empty {
          <li class="none">No games found.</li>
        }
      </ul>
    }
  `,
  styles: `
    .heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
    }
    .search {
      width: 100%;
    }
    .results {
      list-style: none;
      margin: 0;
      padding: 0;
    }
    .result {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 8px 0;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .none {
      padding: 16px 0;
      color: var(--mat-sys-on-surface-variant);
    }
    .message {
      color: var(--mat-sys-primary);
    }
    .error {
      color: var(--mat-sys-error);
    }
  `,
})
export class AddGamePage {
  private readonly api = inject(CollectionApi);
  private timer: ReturnType<typeof setTimeout> | undefined;
  private latestTerm = '';

  protected readonly minLength = MIN_SEARCH_LENGTH;
  protected readonly tooShort = signal(false);
  protected readonly searching = signal(false);
  protected readonly results = signal<GameSearchResult[] | null>(null);
  protected readonly adding = signal<number | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  protected onInput(value: string): void {
    clearTimeout(this.timer);
    const term = value.trim();
    this.latestTerm = term;
    this.tooShort.set(term.length > 0 && term.length < MIN_SEARCH_LENGTH);
    if (term.length < MIN_SEARCH_LENGTH) {
      this.results.set(null);
      return;
    }
    this.timer = setTimeout(() => void this.search(term), SEARCH_DEBOUNCE_MS);
  }

  protected async add(result: GameSearchResult): Promise<void> {
    this.adding.set(result.bggId);
    this.message.set(null);
    this.error.set(null);
    try {
      const added = await this.api.add(result.bggId);
      this.message.set(
        added.alreadyPresent ? `${added.name} is already in your collection.` : `${added.name} was added to your collection.`,
      );
    } catch (error) {
      this.error.set(isBggUnavailable(error) ? BGG_UNAVAILABLE : `Could not add ${result.name}. Please try again.`);
    } finally {
      this.adding.set(null);
    }
  }

  private async search(term: string): Promise<void> {
    this.searching.set(true);
    this.error.set(null);
    this.message.set(null);
    try {
      const found = await this.api.search(term);
      if (term === this.latestTerm) {
        this.results.set(found);
      }
    } catch (error) {
      if (term === this.latestTerm) {
        this.results.set(null);
        this.error.set(isBggUnavailable(error) ? BGG_UNAVAILABLE : 'Searching failed. Please try again.');
      }
    } finally {
      if (term === this.latestTerm) {
        this.searching.set(false);
      }
    }
  }
}

function isBggUnavailable(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.ServiceUnavailable;
}
