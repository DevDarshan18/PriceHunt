import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { ApiError } from '../../../core/errors/api-error';
import { LOG_LEVEL } from '../../../core/logging/logger.service';
import { SUPPLIERS, supplierResult } from '../../../../testing/search-fixtures';
import { SearchEvent, SearchRequest } from '../models/search.models';
import { SearchStreamService } from '../services/search-stream.service';
import { SearchStore } from './search.store';

const REQUEST: SearchRequest = {
  fromLocation: 'Oslo',
  toLocation: 'Bergen',
  fromDate: '2026-11-01',
  toDate: '2026-11-05',
  supplierIds: ['a', 'b'],
};

function started(searchId: string): SearchEvent {
  return { type: 'started', data: { searchId, createdAtUtc: '2026-01-01T00:00:00Z', suppliers: SUPPLIERS.slice(0, 2) } };
}

function supplier(searchId: string, supplierId: string, price: number): SearchEvent {
  return { type: 'supplier', data: supplierResult(supplierId, price, searchId) };
}

function finished(searchId: string, status: 'Completed' | 'TimedOut', respondedCount: number): SearchEvent {
  return { type: 'finished', data: { searchId, status, selectedCount: 2, respondedCount } };
}

describe('SearchStore', () => {
  let store: SearchStore;
  let streams: Subject<SearchEvent>[];

  beforeEach(() => {
    streams = [];
    TestBed.configureTestingModule({
      providers: [
        SearchStore,
        { provide: LOG_LEVEL, useValue: 'error' },
        {
          provide: SearchStreamService,
          useValue: {
            search: () => {
              const stream = new Subject<SearchEvent>();
              streams.push(stream);
              return stream;
            },
          },
        },
      ],
    });
    store = TestBed.inject(SearchStore);
  });

  it('starts idle', () => {
    expect(store.phase()).toBe('idle');
    expect(store.rows()).toEqual([]);
    expect(store.lastRequest()).toBeNull();
  });

  it('remembers the last request so the form can be filled in again', () => {
    store.search(REQUEST);

    expect(store.lastRequest()).toEqual(REQUEST);
  });

  it('shows the suppliers as waiting once the search has started', () => {
    store.search(REQUEST);
    streams[0].next(started('s1'));

    expect(store.phase()).toBe('searching');
    expect(store.selectedCount()).toBe(2);
    expect(store.respondedCount()).toBe(0);
    expect(store.rows().map((row) => row.status)).toEqual(['Waiting', 'Waiting']);
  });

  it('keeps the rows sorted by price while results arrive', () => {
    store.search(REQUEST);
    streams[0].next(started('s1'));

    streams[0].next(supplier('s1', 'a', 300));
    expect(store.rows().map((row) => row.supplierName)).toEqual(['Atlas', 'BlueWave']);

    streams[0].next(supplier('s1', 'b', 100));
    expect(store.rows().map((row) => row.supplierName)).toEqual(['BlueWave', 'Atlas']);
    expect(store.respondedCount()).toBe(2);
  });

  it('ends with the final status and the responded count from the server', () => {
    store.search(REQUEST);
    streams[0].next(started('s1'));
    streams[0].next(supplier('s1', 'a', 300));
    streams[0].next(finished('s1', 'TimedOut', 1));
    streams[0].complete();

    expect(store.phase()).toBe('finished');
    expect(store.finalStatus()).toBe('TimedOut');
    expect(store.respondedCount()).toBe(1);
    expect(store.rows().map((row) => row.status)).toEqual(['Succeeded', 'TimedOut']);
  });

  it('shows how long the search waited for a supplier that never answered', () => {
    spyOn(Date, 'now').and.returnValues(1_000, 7_300);
    store.search(REQUEST);
    streams[0].next(started('s1'));
    streams[0].next(supplier('s1', 'a', 300));
    streams[0].next(finished('s1', 'TimedOut', 1));

    expect(store.rows().map((row) => row.responseTimeMilliseconds)).toEqual([100, 6000]);
  });

  it('cancels the previous search when a new one starts and clears its results', () => {
    store.search(REQUEST);
    streams[0].next(started('s1'));
    streams[0].next(supplier('s1', 'a', 300));

    store.search(REQUEST);

    expect(streams[0].observed).toBeFalse();
    expect(store.rows()).toEqual([]);
    expect(store.respondedCount()).toBe(0);
  });

  it('ignores a late event that belongs to another search', () => {
    store.search(REQUEST);
    streams[0].next(started('new'));

    streams[0].next(supplier('old', 'a', 1));

    expect(store.respondedCount()).toBe(0);
    expect(store.rows().every((row) => row.status === 'Waiting')).toBeTrue();
  });

  it('can be cancelled by the user', () => {
    store.search(REQUEST);
    streams[0].next(started('s1'));

    store.cancel();

    expect(streams[0].observed).toBeFalse();
    expect(store.phase()).toBe('finished');
    expect(store.finalStatus()).toBe('Cancelled');
    expect(store.rows().every((row) => row.status === 'Cancelled')).toBeTrue();
  });

  it('reports a failed request', () => {
    const error = new ApiError(500, 'Server down');
    store.search(REQUEST);

    streams[0].error(error);

    expect(store.phase()).toBe('failed');
    expect(store.error()).toBe(error);
  });

  it('reports a stream that ends before the search finished', () => {
    spyOn(console, 'error');
    store.search(REQUEST);
    streams[0].next(started('s1'));

    streams[0].complete();

    expect(store.phase()).toBe('failed');
    expect(store.error()?.message).toContain('connection ended');
  });
});
