import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, catchError, map, of, switchMap, tap } from 'rxjs';
import { ApiError } from '../../../core/errors/api-error';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { HistoryFilters, HistoryItem, HistoryQuery, HistorySortField } from '../models/history.models';
import { HistoryService } from '../services/history.service';

export const DEFAULT_HISTORY_QUERY: HistoryQuery = {
  from: '',
  to: '',
  supplierIds: [],
  origin: '',
  destination: '',
  sortBy: 'date',
  descending: true,
  page: 1,
  pageSize: 20,
};

type LoadOutcome = { result: PagedResult<HistoryItem> } | { error: ApiError };

@Injectable()
export class HistoryStore {
  private readonly service = inject(HistoryService);
  private readonly requests = new Subject<HistoryQuery>();

  readonly query = signal<HistoryQuery>(DEFAULT_HISTORY_QUERY);
  readonly items = signal<HistoryItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal<ApiError | null>(null);

  constructor() {
    this.requests
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap((query) =>
          this.service.query(query).pipe(
            map((result): LoadOutcome => ({ result })),
            catchError((error: ApiError) => of<LoadOutcome>({ error })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => {
        this.loading.set(false);
        if ('result' in outcome) {
          this.items.set(outcome.result.items);
          this.totalCount.set(outcome.result.totalCount);
        } else {
          this.error.set(outcome.error);
        }
      });

    this.requests.next(this.query());
  }

  applyFilters(filters: HistoryFilters): void {
    this.update({ ...filters, page: 1 });
  }

  sortBy(field: HistorySortField): void {
    const { sortBy, descending } = this.query();
    this.update({ sortBy: field, descending: sortBy === field ? !descending : false, page: 1 });
  }

  goToPage(page: number): void {
    this.update({ page });
  }

  setPageSize(pageSize: number): void {
    this.update({ pageSize, page: 1 });
  }

  private update(changes: Partial<HistoryQuery>): void {
    this.query.update((query) => ({ ...query, ...changes }));
    this.requests.next(this.query());
  }
}
