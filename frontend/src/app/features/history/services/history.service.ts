import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { toHttpParams } from '../../../shared/utils/http-params';
import { HistoryItem, HistoryQuery } from '../models/history.models';

@Injectable({ providedIn: 'root' })
export class HistoryService {
  private readonly http = inject(HttpClient);

  query(query: HistoryQuery): Observable<PagedResult<HistoryItem>> {
    return this.http.get<PagedResult<HistoryItem>>('/api/history', { params: toHttpParams({ ...query }) });
  }
}
