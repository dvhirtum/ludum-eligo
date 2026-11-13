import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { ImportJob } from './settings-api';
import { SettingsPage } from './settings-page';

const job = (status: ImportJob['status'], extra: Partial<ImportJob> = {}): ImportJob => ({
  id: 'job-1',
  status,
  createdAt: '2026-11-13T12:00:00Z',
  startedAt: null,
  finishedAt: null,
  added: null,
  alreadyPresent: null,
  error: null,
  ...extra,
});

describe('SettingsPage', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<SettingsPage>;
  let page: HTMLElement;

  async function render(bggUsername: string | null, latest: ImportJob | null): Promise<void> {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      imports: [SettingsPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(SettingsPage);
    fixture.detectChanges();
    page = fixture.nativeElement as HTMLElement;
    http.expectOne('/api/profile').flush({ bggUsername });
    const latestRequest = http.expectOne('/api/import/latest');
    if (latest) {
      latestRequest.flush(latest);
    } else {
      latestRequest.flush(null, { status: 204, statusText: 'No Content' });
    }
    await tick();
  }

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  async function tick(ms = 0): Promise<void> {
    await vi.advanceTimersByTimeAsync(ms);
    fixture.detectChanges();
  }

  const usernameInput = () => page.querySelector<HTMLInputElement>('input[name="bggUsername"]')!;
  const button = (text: string) => [...page.querySelectorAll('button')].find((b) => b.textContent?.trim() === text)!;
  const status = () => page.querySelector('[data-testid="import-status"]')?.textContent?.replace(/\s+/g, ' ').trim();

  async function saveUsername(value: string): Promise<void> {
    const input = usernameInput();
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await tick();
    button('Save').click();
    await tick();
  }

  it('should show the linked username', async () => {
    await render('meeple42', null);

    expect(usernameInput().value).toBe('meeple42');
  });

  it('should save the username and confirm', async () => {
    await render(null, null);

    await saveUsername('meeple42');
    const request = http.expectOne('/api/profile');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ bggUsername: 'meeple42' });
    request.flush({ bggUsername: 'meeple42' });
    await tick();

    expect(page.textContent).toContain('BGG username saved.');
    expect(button('Import from BGG').disabled).toBe(false);
  });

  it('should show that the username was not found on bgg', async () => {
    await render(null, null);

    await saveUsername('nobody');
    http.expectOne('/api/profile').flush(
      { errors: { bggUsername: ["The user 'nobody' was not found on BGG."] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await tick();

    expect(page.textContent).toContain("The user 'nobody' was not found on BGG.");
    expect(button('Import from BGG').disabled).toBe(true);
  });

  it('should confirm clearing when an empty username is saved', async () => {
    await render('meeple42', null);

    await saveUsername('');
    const request = http.expectOne('/api/profile');
    expect(request.request.body).toEqual({ bggUsername: null });
    request.flush({ bggUsername: null });
    await tick();

    expect(page.textContent).toContain('BGG username removed.');
    expect(button('Import from BGG').disabled).toBe(true);
  });

  it('should disable importing until a username is linked', async () => {
    await render(null, null);

    expect(button('Import from BGG').disabled).toBe(true);
    expect(page.textContent).toContain('Link your BGG username to import');
  });

  it('should poll every 3 seconds while the import is active and show the counts when it succeeds', async () => {
    await render('meeple42', null);

    button('Import from BGG').click();
    const start = http.expectOne('/api/import');
    expect(start.request.method).toBe('POST');
    start.flush(job('Queued'), { status: 202, statusText: 'Accepted' });
    await tick();
    expect(status()).toContain('queued');
    expect(button('Import from BGG').disabled).toBe(true);

    await tick(2999);
    http.expectNone('/api/import/latest');
    await tick(1);
    http.expectOne('/api/import/latest').flush(job('Running'));
    await tick();
    expect(status()).toContain('Importing');

    await tick(3000);
    http.expectOne('/api/import/latest').flush(job('Succeeded', { added: 12, alreadyPresent: 3 }));
    await tick();
    expect(status()).toContain('12 games added');
    expect(status()).toContain('3 already in your collection');

    await tick(10000);
    http.expectNone('/api/import/latest');
    expect(button('Import from BGG').disabled).toBe(false);
  });

  it('should resume polling when an import is already running on load', async () => {
    await render('meeple42', job('Running'));

    await tick(3000);
    http.expectOne('/api/import/latest').flush(job('Running'));
  });

  it('should show the reason and allow a new import when the import failed', async () => {
    await render('meeple42', job('Failed', { error: 'BGG did not return the collection (Collection is private).' }));

    expect(status()).toContain('Import failed');
    expect(status()).toContain('BGG did not return the collection (Collection is private).');
    expect(button('Import from BGG').disabled).toBe(false);
    await tick(10000);
    http.expectNone('/api/import/latest');
  });
});
