import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { typeInto } from '../../../testing/dom-helpers';
import { configureHttp } from '../../../testing/http-testing';
import { LOCATIONS, placesLoader, SUPPLIERS, supplierResult } from '../../../testing/search-fixtures';
import { LocationsService } from '../../shared/data-access/locations.service';
import { ApiError } from '../../core/errors/api-error';
import { SearchEvent, SearchRequest } from './models/search.models';
import { SearchPageComponent } from './search-page.component';
import { SearchStreamService } from './services/search-stream.service';

describe('SearchPageComponent', () => {
  let fixture: ComponentFixture<SearchPageComponent>;
  let stream: Subject<SearchEvent>;
  let requests: SearchRequest[];

  const element = () => fixture.nativeElement as HTMLElement;

  function search(): void {
    typeInto(element(), '#fromLocation', 'Rotterdam');
    typeInto(element(), '#toLocation', 'Hamburg');
    element().querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  beforeEach(() => {
    stream = new Subject<SearchEvent>();
    requests = [];
    configureHttp();
    TestBed.configureTestingModule({
      providers: [
        { provide: LocationsService, useValue: { find: placesLoader(LOCATIONS) } },
        {
          provide: SearchStreamService,
          useValue: {
            search: (request: SearchRequest) => {
              requests.push(request);
              return stream;
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(SearchPageComponent);
    const controller = TestBed.inject(HttpTestingController);
    controller.expectOne('/api/suppliers').flush(SUPPLIERS);
    fixture.detectChanges();
    fixture.detectChanges();
  });

  it('shows no results until a search is started', () => {
    expect(element().querySelector('app-results-list')).toBeNull();
  });

  it('starts a search with the typed locations and every supplier', () => {
    search();

    expect(requests.length).toBe(1);
    expect(requests[0]).toEqual(
      jasmine.objectContaining({
        fromLocation: 'Rotterdam',
        toLocation: 'Hamburg',
        supplierIds: SUPPLIERS.map((supplier) => supplier.id),
      }),
    );
  });

  it('shows the progress and each price as it arrives', () => {
    search();

    stream.next({ type: 'started', data: { searchId: 's1', createdAtUtc: '2026-01-01T00:00:00Z', suppliers: SUPPLIERS.slice(0, 2) } });
    stream.next({ type: 'supplier', data: supplierResult('b', 100) });
    fixture.detectChanges();

    expect(element().textContent).toContain('1 of 2 suppliers responded');
    const rows = Array.from(element().querySelectorAll('.result')).map((row) => row.textContent?.replace(/\s+/g, ' ').trim());
    expect(rows[0]).toContain('BlueWave');
    expect(rows[0]).toContain('$100.00');
    expect(rows[1]).toContain('Atlas');
    expect(rows[1]).toContain('Waiting');
  });

  it('shows the final state when the search ends', () => {
    search();
    stream.next({ type: 'started', data: { searchId: 's1', createdAtUtc: '2026-01-01T00:00:00Z', suppliers: SUPPLIERS.slice(0, 2) } });
    stream.next({ type: 'finished', data: { searchId: 's1', status: 'TimedOut', selectedCount: 2, respondedCount: 0 } });
    fixture.detectChanges();

    expect(element().querySelector('app-search-progress')?.textContent).toContain('Timed out');
  });

  it('shows the message when the search fails', () => {
    search();

    stream.error(new ApiError(500, 'The server is down.'));
    fixture.detectChanges();

    expect(element().querySelector('app-error-message')?.textContent).toContain('The server is down.');
  });
});
