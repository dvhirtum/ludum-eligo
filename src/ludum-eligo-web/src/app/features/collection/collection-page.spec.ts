import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { CollectionGame } from './collection-api';
import { CollectionPage } from './collection-page';
import { settle } from '../../testing/settle';

const game = (bggId: number, name: string): CollectionGame => ({
  bggId,
  name,
  yearPublished: 2017,
  imageUrl: null,
  thumbnailUrl: `https://images.test/${bggId}.png`,
  minPlayers: 2,
  maxPlayers: 4,
  minPlaytime: 30,
  maxPlaytime: 45,
});

describe('CollectionPage', () => {
  let http: HttpTestingController;

  async function render(games: CollectionGame[]) {
    TestBed.configureTestingModule({
      imports: [CollectionPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(CollectionPage);
    fixture.detectChanges();
    http.expectOne('/api/collection').flush(games);
    await settle(fixture);
    return { fixture, page: fixture.nativeElement as HTMLElement };
  }

  const names = (page: HTMLElement) => [...page.querySelectorAll('[data-testid="game-name"]')].map((e) => e.textContent?.trim());

  afterEach(() => http.verify());

  it('should list the games with players and play time', async () => {
    const { page } = await render([game(1, 'Azul'), { ...game(2, 'Catan'), minPlaytime: 60, maxPlaytime: 60, maxPlayers: null }]);

    expect(names(page)).toEqual(['Azul', 'Catan']);
    expect(page.textContent).toContain('2–4 players');
    expect(page.textContent).toContain('30–45 min');
    expect(page.textContent).toContain('2+ players');
    expect(page.textContent).toContain('60 min');
  });

  it('should show only matching games ignoring case when filtering', async () => {
    const { fixture, page } = await render([game(1, 'Azul'), game(2, 'Ticket to Ride'), game(3, 'Ticket to Ride: Europe')]);

    const filter = page.querySelector<HTMLInputElement>('input[name="filter"]')!;
    filter.value = 'TICKET';
    filter.dispatchEvent(new Event('input'));
    await settle(fixture);

    expect(names(page)).toEqual(['Ticket to Ride', 'Ticket to Ride: Europe']);
  });

  it('should say nothing matches when the filter matches no game', async () => {
    const { fixture, page } = await render([game(1, 'Azul')]);

    const filter = page.querySelector<HTMLInputElement>('input[name="filter"]')!;
    filter.value = 'catan';
    filter.dispatchEvent(new Event('input'));
    await settle(fixture);

    expect(names(page)).toEqual([]);
    expect(page.textContent).toContain('No games match');
  });

  it('should offer to search and to import from bgg when the collection is empty', async () => {
    const { page } = await render([]);

    expect(page.textContent).toContain('Your collection is empty');
    const links = [...page.querySelectorAll('a')].map((a) => [a.textContent?.trim(), a.getAttribute('href')]);
    expect(links).toContainEqual(['Search BGG', '/collection/add']);
    expect(links).toContainEqual(['Import from BGG', '/settings']);
    expect(page.querySelector('input[name="filter"]')).toBeNull();
  });

  it('should remove the game from the list when remove is clicked', async () => {
    const { fixture, page } = await render([game(1, 'Azul'), game(2, 'Catan')]);

    page.querySelector<HTMLButtonElement>('button[aria-label="Remove Azul"]')!.click();
    const request = http.expectOne('/api/collection/1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await settle(fixture);

    expect(names(page)).toEqual(['Catan']);
  });

  it('should keep the game and show an error when removing fails', async () => {
    const { fixture, page } = await render([game(1, 'Azul')]);

    page.querySelector<HTMLButtonElement>('button[aria-label="Remove Azul"]')!.click();
    http.expectOne('/api/collection/1').flush(null, { status: 500, statusText: 'Server Error' });
    await settle(fixture);

    expect(names(page)).toEqual(['Azul']);
    expect(page.textContent).toContain('Could not remove Azul');
  });
});
