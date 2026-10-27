import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { ApiError } from '../../../core/errors/api-error';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { HistoryItem, HistoryQuery } from '../models/history.models';
import { HistoryService } from '../services/history.service';
import { DEFAULT_HISTORY_QUERY, HistoryStore } from './history.store';

function item(supplierName: string): HistoryItem {
  return {
    searchId: 's1',
    searchedAtUtc: '2026-01-01T00:00:00Z',
    origin: 'Oslo',
    destination: 'Bergen',
    supplierId: supplierName,
    supplierName,
    price: 10,
    currency: 'USD',
    responseTimeMilliseconds: 100,
    status: 'Succeeded',
    errorCode: null,
  };
}

describe('HistoryStore', () => {
  let queries: HistoryQuery[];
  let respond: (query: HistoryQuery) => Observable<PagedResult<HistoryItem>>;

  function createStore(): HistoryStore {
    TestBed.configureTestingModule({
      providers: [
        HistoryStore,
        {
          provide: HistoryService,
          useValue: {
            query: (query: HistoryQuery) => {
              queries.push(query);
              return respond(query);
            },
          },
        },
      ],
    });
    return TestBed.inject(HistoryStore);
  }

  beforeEach(() => {
    queries = [];
    respond = () => of({ items: [item('Atlas')], page: 1, pageSize: 20, totalCount: 45 });
  });

  it('loads the first page, newest first, when it is created', () => {
    const store = createStore();

    expect(queries).toEqual([DEFAULT_HISTORY_QUERY]);
    expect(store.items().map((row) => row.supplierName)).toEqual(['Atlas']);
    expect(store.totalCount()).toBe(45);
    expect(store.loading()).toBeFalse();
  });

  it('applies filters from the first page', () => {
    const store = createStore();
    store.goToPage(3);

    store.applyFilters({ from: '2026-01-01', to: '2026-01-31', supplierIds: ['a'], origin: 'Oslo', destination: '' });

    expect(queries.at(-1)).toEqual(
      jasmine.objectContaining({ from: '2026-01-01', supplierIds: ['a'], origin: 'Oslo', page: 1 }),
    );
  });

  it('sorts a new column ascending and reverses the order when it is clicked again', () => {
    const store = createStore();

    store.sortBy('price');
    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ sortBy: 'price', descending: false }));

    store.sortBy('price');
    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ sortBy: 'price', descending: true }));

    store.sortBy('route');
    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ sortBy: 'route', descending: false }));
  });

  it('goes back to the first page when the sort or the page size changes', () => {
    const store = createStore();
    store.goToPage(4);

    store.setPageSize(50);
    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ pageSize: 50, page: 1 }));

    store.goToPage(2);
    store.sortBy('supplier');
    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ page: 1 }));
  });

  it('keeps the error and stops loading when the request fails', () => {
    const error = new ApiError(500, 'Server down');
    respond = () => throwError(() => error);

    const store = createStore();

    expect(store.error()).toBe(error);
    expect(store.loading()).toBeFalse();
  });

  it('only shows the answer to the latest request', () => {
    const first = new Subject<PagedResult<HistoryItem>>();
    const second = new Subject<PagedResult<HistoryItem>>();
    const answers = [first, second];
    respond = () => answers.shift()!;
    const store = createStore();

    store.goToPage(2);
    second.next({ items: [item('Latest')], page: 2, pageSize: 20, totalCount: 40 });
    first.next({ items: [item('Stale')], page: 1, pageSize: 20, totalCount: 40 });

    expect(store.items().map((row) => row.supplierName)).toEqual(['Latest']);
  });
});
