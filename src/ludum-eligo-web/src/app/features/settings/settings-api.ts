import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Profile {
  bggUsername: string | null;
}

export interface ImportJob {
  id: string;
  status: 'Queued' | 'Running' | 'Succeeded' | 'Failed';
  createdAt: string;
  startedAt: string | null;
  finishedAt: string | null;
  added: number | null;
  alreadyPresent: number | null;
  error: string | null;
}

/** The profile and import endpoints. */
@Injectable({ providedIn: 'root' })
export class SettingsApi {
  private readonly http = inject(HttpClient);

  getProfile(): Promise<Profile> {
    return firstValueFrom(this.http.get<Profile>('/api/profile'));
  }

  setBggUsername(bggUsername: string | null): Promise<Profile> {
    return firstValueFrom(this.http.put<Profile>('/api/profile', { bggUsername }));
  }

  startImport(): Promise<ImportJob> {
    return firstValueFrom(this.http.post<ImportJob>('/api/import', null));
  }

  /** The most recent import, or null when there is none (204). */
  latestImport(): Promise<ImportJob | null> {
    return firstValueFrom(this.http.get<ImportJob | null>('/api/import/latest'));
  }
}
