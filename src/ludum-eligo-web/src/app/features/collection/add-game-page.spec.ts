import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { AddGamePage } from './add-game-page';

describe('AddGamePage', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<AddGamePage>;
  let page: HTMLElement;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      imports: [AddGamePage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AddGamePage);
    fixture.detectChanges();
    page = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  /** Runs timers and promise continuations, then renders. */
  async function tick(ms = 0): Promise<void> {
    await vi.advanceTimersByTimeAsync(ms);
    fixture.detectChanges();
  }

  async function type(term: string): Promise<void> {
    const input = page.querySelector<HTMLInputElement>('input[name="search"]')!;
    input.value = term;
    input.dispatchEvent(new Event('input'));
    await tick();
  }

  async function searchFor(term: string, results: unknown[]): Promise<void> {
    await type(term);
    await tick(300);
    http.expectOne((r) => r.url === '/api/games/search' && r.params.get('q') === term).flush(results);
    await tick();
  }

  const button = (label: string) => page.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`)!;

  it('should search only after typing pauses', async () => {
    await type('Azu');
    await tick(200);
    await type('Azul');
    await tick(299);
    http.expectNone('/api/games/search');

    await tick(1);
    const request = http.expectOne((r) => r.url === '/api/games/search');
    expect(request.request.params.get('q')).toBe('Azul');
    request.flush([]);
  });

  it('should not search and ask for more characters when the term is shorter than 3 characters', async () => {
    await type('Az');
    await tick(1000);

    http.expectNone((r) => r.url === '/api/games/search');
    expect(page.textContent).toContain('at least 3 characters');
  });

  it('should list the results with their year', async () => {
    await searchFor('Azul', [
      { bggId: 230802, name: 'Azul', yearPublished: 2017 },
      { bggId: 999001, name: 'Azul Fan Variant', yearPublished: null },
    ]);

    const items = [...page.querySelectorAll('[data-testid="result"]')].map((e) => e.textContent?.replace(/\s+/g, ' ').trim());
    expect(items[0]).toContain('Azul (2017)');
    expect(items[1]).toContain('Azul Fan Variant');
    expect(items[1]).not.toContain('(');
  });

  it('should say nothing was found when the search has no results', async () => {
    await searchFor('Nothing at all', []);

    expect(page.textContent).toContain('No games found');
  });

  it('should show that bgg is unavailable when the search fails with 503', async () => {
    await type('Azul');
    await tick(300);
    http.expectOne((r) => r.url === '/api/games/search').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await tick();

    expect(page.textContent).toContain('BoardGameGeek is unavailable. Try again later.');
  });

  it('should confirm when a game is added', async () => {
    await searchFor('Azul', [{ bggId: 230802, name: 'Azul', yearPublished: 2017 }]);

    button('Add Azul').click();
    const request = http.expectOne('/api/collection');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ bggId: 230802 });
    request.flush({ bggId: 230802, name: 'Azul', alreadyPresent: false });
    await tick();

    expect(page.textContent).toContain('Azul was added to your collection.');
  });

  it('should say the game is already in the collection when it was there', async () => {
    await searchFor('Azul', [{ bggId: 230802, name: 'Azul', yearPublished: 2017 }]);

    button('Add Azul').click();
    http.expectOne('/api/collection').flush({ bggId: 230802, name: 'Azul', alreadyPresent: true });
    await tick();

    expect(page.textContent).toContain('Azul is already in your collection.');
  });

  it('should show that bgg is unavailable when adding fails with 503', async () => {
    await searchFor('Azul', [{ bggId: 230802, name: 'Azul', yearPublished: 2017 }]);

    button('Add Azul').click();
    http.expectOne('/api/collection').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await tick();

    expect(page.textContent).toContain('BoardGameGeek is unavailable. Try again later.');
  });
});
