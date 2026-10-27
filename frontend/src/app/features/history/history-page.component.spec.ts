import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { buttonLabelled, typeInto } from '../../../testing/dom-helpers';
import { configureHttp } from '../../../testing/http-testing';
import { LOCATIONS, placesLoader, SUPPLIERS } from '../../../testing/search-fixtures';
import { LocationsService } from '../../shared/data-access/locations.service';
import { ApiError } from '../../core/errors/api-error';
import { PagedResult } from '../../shared/models/paged-result.model';
import { HistoryPageComponent } from './history-page.component';
import { HistoryItem, HistoryQuery } from './models/history.models';
import { HistoryService } from './services/history.service';

const ITEM: HistoryItem = {
  searchId: 's1',
  searchedAtUtc: '2026-05-01T10:00:00Z',
  origin: 'Rotterdam',
  destination: 'Hamburg',
  supplierId: 'a',
  supplierName: 'Atlas',
  price: 120.5,
  currency: 'USD',
  responseTimeMilliseconds: 900,
  status: 'Succeeded',
  errorCode: null,
};

describe('HistoryPageComponent', () => {
  let fixture: ComponentFixture<HistoryPageComponent>;
  let queries: HistoryQuery[];
  let answer: () => ReturnType<HistoryService['query']>;

  const element = () => fixture.nativeElement as HTMLElement;

  function create(): void {
    configureHttp();
    TestBed.configureTestingModule({
      providers: [
        { provide: LocationsService, useValue: { find: placesLoader(LOCATIONS) } },
        {
          provide: HistoryService,
          useValue: {
            query: (query: HistoryQuery) => {
              queries.push(query);
              return answer();
            },
          },
        },
      ],
    });
    fixture = TestBed.createComponent(HistoryPageComponent);
    const controller = TestBed.inject(HttpTestingController);
    controller.expectOne('/api/suppliers').flush(SUPPLIERS);
    fixture.detectChanges();
    fixture.detectChanges();
  }

  beforeEach(() => {
    queries = [];
    const page: PagedResult<HistoryItem> = { items: [ITEM], page: 1, pageSize: 20, totalCount: 45 };
    answer = () => of(page);
  });

  it('loads and shows the newest results first', () => {
    create();

    expect(queries[0]).toEqual(jasmine.objectContaining({ sortBy: 'date', descending: true, page: 1 }));
    expect(element().querySelector('tbody')?.textContent).toContain('Rotterdam → Hamburg');
    expect(element().textContent).toContain('45 results');
  });

  it('asks for the data sorted when a column header is clicked', () => {
    create();

    element().querySelectorAll<HTMLButtonElement>('th .sort')[4].click();

    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ sortBy: 'price', descending: false, page: 1 }));
  });

  it('asks for the next page when next is clicked', () => {
    create();

    buttonLabelled(element(), 'Next').click();

    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ page: 2 }));
  });

  it('applies the filters from the first page', () => {
    create();
    buttonLabelled(element(), 'Next').click();

    typeInto(element(), '#origin', 'Oslo');
    buttonLabelled(element(), 'Apply').click();

    expect(queries.at(-1)).toEqual(jasmine.objectContaining({ origin: 'Oslo', page: 1 }));
  });

  it('shows the message when the history cannot be loaded', () => {
    answer = () => throwError(() => new ApiError(500, 'History is unavailable.'));

    create();

    expect(element().querySelector('app-error-message')?.textContent).toContain('History is unavailable.');
  });
});
