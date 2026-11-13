import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { CollectionApi, CollectionGame } from './collection-api';
import { formatPlayers, formatPlaytime } from './game-format';

/** The signed-in user's games, with a client-side name filter. */
@Component({
  selector: 'app-collection-page',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="heading">
      <h1>Your collection</h1>
      <a mat-flat-button routerLink="/collection/add">Add game</a>
    </header>

    @if (error(); as message) {
      <p class="error" role="alert">{{ message }}</p>
    }

    @if (games(); as all) {
      @if (all.length === 0) {
        <section class="empty">
          <p>Your collection is empty.</p>
          <p>Find games on BoardGameGeek, or import the collection you keep there.</p>
          <a mat-stroked-button routerLink="/collection/add">Search BGG</a>
          <a mat-stroked-button routerLink="/settings">Import from BGG</a>
        </section>
      } @else {
        <mat-form-field class="filter" appearance="outline" subscriptSizing="dynamic">
          <mat-label>Filter by name</mat-label>
          <input matInput name="filter" autocomplete="off" [value]="filter()" (input)="filter.set($any($event.target).value)" />
        </mat-form-field>

        <ul class="games">
          @for (game of visibleGames(); track game.bggId) {
            <li class="game">
              @if (game.thumbnailUrl) {
                <img [src]="game.thumbnailUrl" alt="" width="64" height="64" loading="lazy" />
              } @else {
                <span class="no-image" aria-hidden="true"></span>
              }
              <div class="details">
                <span class="name" data-testid="game-name">{{ game.name }}</span>
                <span class="meta">
                  @if (game.yearPublished) {
                    <span>{{ game.yearPublished }}</span>
                  }
                  @if (players(game); as text) {
                    <span>{{ text }}</span>
                  }
                  @if (playtime(game); as text) {
                    <span>{{ text }}</span>
                  }
                </span>
              </div>
              <button mat-button type="button" [attr.aria-label]="'Remove ' + game.name" [disabled]="removing() === game.bggId" (click)="remove(game)">
                Remove
              </button>
            </li>
          } @empty {
            <li class="no-match">No games match "{{ filter() }}".</li>
          }
        </ul>
      }
    } @else if (!error()) {
      <mat-progress-bar mode="indeterminate" />
    }
  `,
  styles: `
    .heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
    }
    .filter {
      width: 100%;
      margin-bottom: 8px;
    }
    .games {
      list-style: none;
      margin: 0;
      padding: 0;
    }
    .game {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 8px 0;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    img,
    .no-image {
      width: 64px;
      height: 64px;
      object-fit: contain;
      flex: none;
      border-radius: 4px;
      background: var(--mat-sys-surface-container);
    }
    .details {
      display: flex;
      flex-direction: column;
      flex: 1 1 auto;
      min-width: 0;
    }
    .name {
      font: var(--mat-sys-title-medium);
    }
    .meta {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      color: var(--mat-sys-on-surface-variant);
      font: var(--mat-sys-body-medium);
    }
    .empty a {
      margin-right: 8px;
    }
    .no-match {
      padding: 16px 0;
      color: var(--mat-sys-on-surface-variant);
    }
    .error {
      color: var(--mat-sys-error);
    }
  `,
})
export class CollectionPage {
  private readonly api = inject(CollectionApi);

  protected readonly games = signal<CollectionGame[] | null>(null);
  protected readonly filter = signal('');
  protected readonly removing = signal<number | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly visibleGames = computed(() => {
    const term = this.filter().trim().toLocaleLowerCase();
    const games = this.games() ?? [];
    return term ? games.filter((g) => g.name.toLocaleLowerCase().includes(term)) : games;
  });

  protected readonly players = (game: CollectionGame) => formatPlayers(game.minPlayers, game.maxPlayers);
  protected readonly playtime = (game: CollectionGame) => formatPlaytime(game.minPlaytime, game.maxPlaytime);

  constructor() {
    void this.load();
  }

  protected async remove(game: CollectionGame): Promise<void> {
    this.removing.set(game.bggId);
    this.error.set(null);
    try {
      await this.api.remove(game.bggId);
      this.games.update((games) => games?.filter((g) => g.bggId !== game.bggId) ?? null);
    } catch {
      this.error.set(`Could not remove ${game.name}. Please try again.`);
    } finally {
      this.removing.set(null);
    }
  }

  private async load(): Promise<void> {
    try {
      this.games.set(await this.api.list());
    } catch {
      this.error.set('Could not load your collection. Please reload the page.');
    }
  }
}
