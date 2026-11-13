import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CollectionGame {
  bggId: number;
  name: string;
  yearPublished: number | null;
  imageUrl: string | null;
  thumbnailUrl: string | null;
  minPlayers: number | null;
  maxPlayers: number | null;
  minPlaytime: number | null;
  maxPlaytime: number | null;
}

export interface GameSearchResult {
  bggId: number;
  name: string;
  yearPublished: number | null;
}

export interface AddGameResult {
  bggId: number;
  name: string;
  alreadyPresent: boolean;
}

/** The collection and BGG search endpoints. */
@Injectable({ providedIn: 'root' })
export class CollectionApi {
  private readonly http = inject(HttpClient);

  list(): Promise<CollectionGame[]> {
    return firstValueFrom(this.http.get<CollectionGame[]>('/api/collection'));
  }

  add(bggId: number): Promise<AddGameResult> {
    return firstValueFrom(this.http.post<AddGameResult>('/api/collection', { bggId }));
  }

  remove(bggId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/collection/${bggId}`));
  }

  search(term: string): Promise<GameSearchResult[]> {
    return firstValueFrom(this.http.get<GameSearchResult[]>('/api/games/search', { params: { q: term } }));
  }
}
