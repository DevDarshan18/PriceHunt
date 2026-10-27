import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { ApiError } from '../../../core/errors/api-error';
import { LoggerService } from '../../../core/logging/logger.service';
import { SEARCH_RULES } from '../../../shared/forms/search-rules';
import { Supplier } from '../../../shared/models/supplier.model';
import { FinalSearchStatus, SearchEvent, SearchRequest, SupplierResultEvent } from '../models/search.models';
import { SearchStreamService } from '../services/search-stream.service';
import { buildRows } from './search-rows';

export type SearchPhase = 'idle' | 'searching' | 'finished' | 'failed';

interface SearchState {
  phase: SearchPhase;
  searchId: string | null;
  suppliers: Supplier[];
  results: SupplierResultEvent[];
  finalStatus: FinalSearchStatus | null;
  respondedCount: number | null;
  startedAtMilliseconds: number | null;
  waitedMilliseconds: number | null;
  error: ApiError | null;
}

const INITIAL_STATE: SearchState = {
  phase: 'idle',
  searchId: null,
  suppliers: [],
  results: [],
  finalStatus: null,
  respondedCount: null,
  startedAtMilliseconds: null,
  waitedMilliseconds: null,
  error: null,
};

@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly stream = inject(SearchStreamService);
  private readonly log = inject(LoggerService).scoped('Search');
  private readonly state = signal<SearchState>(INITIAL_STATE);
  private readonly request = signal<SearchRequest | null>(null);
  private subscription?: Subscription;

  readonly lastRequest = this.request.asReadonly();
  readonly phase = computed(() => this.state().phase);
  readonly error = computed(() => this.state().error);
  readonly finalStatus = computed(() => this.state().finalStatus);
  readonly selectedCount = computed(() => this.state().suppliers.length);
  readonly respondedCount = computed(() => this.state().respondedCount ?? this.state().results.length);
  readonly rows = computed(() => {
    const { suppliers, results, phase, finalStatus, waitedMilliseconds } = this.state();
    return buildRows(suppliers, results, phase === 'searching', finalStatus, waitedMilliseconds);
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => this.subscription?.unsubscribe());
  }

  search(request: SearchRequest): void {
    this.subscription?.unsubscribe();
    this.request.set(request);
    this.state.set({ ...INITIAL_STATE, phase: 'searching', startedAtMilliseconds: Date.now() });
    this.log.info('Starting a search', request);

    this.subscription = this.stream.search(request).subscribe({
      next: (event) => this.apply(event),
      error: (error: ApiError) => this.patch({ phase: 'failed', error }),
      complete: () => this.endUnlessFinished(),
    });
  }

  cancel(): void {
    this.subscription?.unsubscribe();
    this.state.update((state) =>
      state.phase === 'searching'
        ? { ...state, phase: 'finished', finalStatus: 'Cancelled', waitedMilliseconds: this.waitedSince(state.startedAtMilliseconds) }
        : state,
    );
    this.log.info('Search cancelled by the user');
  }

  private apply(event: SearchEvent): void {
    if (event.type === 'started') {
      this.patch({ searchId: event.data.searchId, suppliers: event.data.suppliers });
      return;
    }

    if (event.data.searchId !== this.state().searchId) {
      this.log.warn('Ignored an event from another search', event);
      return;
    }

    if (event.type === 'supplier') {
      this.state.update((state) => ({ ...state, results: [...state.results, event.data] }));
      return;
    }

    this.patch({
      phase: 'finished',
      finalStatus: event.data.status,
      respondedCount: event.data.respondedCount,
      waitedMilliseconds: this.waitedSince(this.state().startedAtMilliseconds),
    });
    this.log.info(`Search ${event.data.status}`, { responded: event.data.respondedCount, selected: event.data.selectedCount });
  }

  private endUnlessFinished(): void {
    if (this.state().phase !== 'searching') {
      return;
    }

    const error = new ApiError(0, 'The connection ended before the search finished.');
    this.log.error(error.message);
    this.patch({ phase: 'failed', error });
  }

  private waitedSince(startedAt: number | null): number | null {
    return startedAt === null ? null : Math.min(Date.now() - startedAt, SEARCH_RULES.timeLimitMilliseconds);
  }

  private patch(changes: Partial<SearchState>): void {
    this.state.update((state) => ({ ...state, ...changes }));
  }
}
