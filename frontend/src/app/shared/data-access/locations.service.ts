import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { PagedResult } from '../models/paged-result.model';

const PAGE_SIZE = 10;

@Injectable({ providedIn: 'root' })
export class LocationsService {
  private readonly http = inject(HttpClient);
  private firstPage?: Observable<PagedResult<string>>;

  readonly find = (search: string, page: number): Observable<PagedResult<string>> => {
    if (search !== '' || page !== 1) {
      return this.request(search, page);
    }

    this.firstPage ??= this.request(search, page).pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.firstPage;
  };

  private request(search: string, page: number): Observable<PagedResult<string>> {
    return this.http.get<PagedResult<string>>('/api/locations', { params: { search, page, pageSize: PAGE_SIZE } });
  }
}
