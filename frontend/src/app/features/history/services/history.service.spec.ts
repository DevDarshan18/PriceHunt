import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../../testing/http-testing';
import { DEFAULT_HISTORY_QUERY } from '../state/history.store';
import { HistoryService } from './history.service';

describe('HistoryService', () => {
  let service: HistoryService;
  let controller: HttpTestingController;

  beforeEach(() => {
    configureHttp();
    service = TestBed.inject(HistoryService);
    controller = TestBed.inject(HttpTestingController);
  });

  it('sends the filters, sort and paging as a query string', () => {
    service
      .query({
        ...DEFAULT_HISTORY_QUERY,
        from: '2026-01-01',
        to: '2026-01-31',
        supplierIds: ['a', 'b'],
        origin: 'Oslo',
        destination: 'Bergen',
        sortBy: 'price',
        descending: false,
        page: 2,
        pageSize: 50,
      })
      .subscribe();

    const { params } = controller.expectOne((request) => request.url === '/api/history').request;

    expect(params.get('from')).toBe('2026-01-01');
    expect(params.get('to')).toBe('2026-01-31');
    expect(params.getAll('supplierIds')).toEqual(['a', 'b']);
    expect(params.get('origin')).toBe('Oslo');
    expect(params.get('destination')).toBe('Bergen');
    expect(params.get('sortBy')).toBe('price');
    expect(params.get('descending')).toBe('false');
    expect(params.get('page')).toBe('2');
    expect(params.get('pageSize')).toBe('50');
  });

  it('leaves out the filters that are empty', () => {
    service.query(DEFAULT_HISTORY_QUERY).subscribe();

    const { params } = controller.expectOne((request) => request.url === '/api/history').request;

    expect(params.keys().sort()).toEqual(['descending', 'page', 'pageSize', 'sortBy']);
  });

  it('returns the page the api sends back', () => {
    let totalCount = 0;
    service.query(DEFAULT_HISTORY_QUERY).subscribe((result) => (totalCount = result.totalCount));

    controller.expectOne((request) => request.url === '/api/history').flush({ items: [], page: 1, pageSize: 20, totalCount: 45 });

    expect(totalCount).toBe(45);
  });
});
