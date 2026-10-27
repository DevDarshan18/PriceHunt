import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../testing/http-testing';
import { LocationsService } from './locations.service';

describe('LocationsService', () => {
  let controller: HttpTestingController;
  let service: LocationsService;

  beforeEach(() => {
    configureHttp();
    controller = TestBed.inject(HttpTestingController);
    service = TestBed.inject(LocationsService);
  });

  it('asks for ten places of the page at a time, with the text typed so far', () => {
    let received: unknown;
    service.find('ham', 2).subscribe((page) => (received = page));

    const request = controller.expectOne((candidate) => candidate.url === '/api/locations');
    expect(request.request.params.get('search')).toBe('ham');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('10');

    const page = { items: ['Hamburg'], page: 2, pageSize: 10, totalCount: 11 };
    request.flush(page);
    expect(received).toEqual(page);
  });

  it('asks for the first page only once when several fields open their lists', () => {
    const firstPage = { items: ['Amsterdam'], page: 1, pageSize: 10, totalCount: 48 };
    const received: unknown[] = [];

    service.find('', 1).subscribe((page) => received.push(page));
    service.find('', 1).subscribe((page) => received.push(page));
    controller.expectOne('/api/locations?search=&page=1&pageSize=10').flush(firstPage);
    service.find('', 1).subscribe((page) => received.push(page));

    expect(received).toEqual([firstPage, firstPage, firstPage]);
    controller.expectNone('/api/locations?search=&page=1&pageSize=10');
  });

  it('does not reuse the first page for a search or for the next page', () => {
    service.find('', 1).subscribe();
    service.find('', 2).subscribe();
    service.find('ham', 1).subscribe();

    expect(controller.match((candidate) => candidate.url === '/api/locations').length).toBe(3);
  });

  it('tries again after the first page could not be loaded', () => {
    service.find('', 1).subscribe({ error: () => undefined });
    controller.expectOne('/api/locations?search=&page=1&pageSize=10').flush(null, { status: 500, statusText: 'Server Error' });

    service.find('', 1).subscribe();

    controller.expectOne('/api/locations?search=&page=1&pageSize=10');
  });
});
