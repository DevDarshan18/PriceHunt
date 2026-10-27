import { HttpEvent, HttpEventType } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Subscription } from 'rxjs';
import { configureHttp } from '../../../../testing/http-testing';
import { SearchEvent, SearchRequest } from '../models/search.models';
import { SearchStreamService } from './search-stream.service';

const REQUEST: SearchRequest = {
  fromLocation: 'Oslo',
  toLocation: 'Bergen',
  fromDate: '2026-11-01',
  toDate: '2026-11-05',
  supplierIds: ['a'],
};

const STARTED = 'event: started\ndata: {"searchId":"s1","createdAtUtc":"2026-01-01T00:00:00Z","suppliers":[{"id":"a","name":"Atlas"}]}\n\n';
const SUPPLIER = 'event: supplier\ndata: {"searchId":"s1","supplierId":"a","supplierName":"Atlas","status":"Succeeded","price":10,"currency":"USD","responseTimeMilliseconds":500,"errorCode":null}\n\n';
const FINISHED = 'event: finished\ndata: {"searchId":"s1","status":"Completed","selectedCount":1,"respondedCount":1}\n\n';

describe('SearchStreamService', () => {
  let controller: HttpTestingController;
  let received: SearchEvent[];
  let subscription: Subscription;

  function progress(partialText: string): HttpEvent<string> {
    return { type: HttpEventType.DownloadProgress, loaded: partialText.length, partialText } as HttpEvent<string>;
  }

  beforeEach(() => {
    configureHttp();
    controller = TestBed.inject(HttpTestingController);
    received = [];
    subscription = TestBed.inject(SearchStreamService)
      .search(REQUEST)
      .subscribe((event) => received.push(event));
  });

  it('posts the search to the api', () => {
    const request = controller.expectOne('/api/searches');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(REQUEST);
  });

  it('emits each event as soon as its text has arrived', () => {
    const request = controller.expectOne('/api/searches');

    request.event(progress(STARTED));
    expect(received.map((event) => event.type)).toEqual(['started']);

    request.event(progress(STARTED + SUPPLIER.slice(0, 20)));
    expect(received.length).toBe(1);

    request.event(progress(STARTED + SUPPLIER));
    expect(received.map((event) => event.type)).toEqual(['started', 'supplier']);
  });

  it('does not repeat events when the final response contains the whole text', () => {
    const request = controller.expectOne('/api/searches');

    request.event(progress(STARTED + SUPPLIER));
    request.flush(STARTED + SUPPLIER + FINISHED);

    expect(received.map((event) => event.type)).toEqual(['started', 'supplier', 'finished']);
  });

  it('parses the event data', () => {
    controller.expectOne('/api/searches').flush(STARTED + SUPPLIER);

    expect(received[1]).toEqual(
      jasmine.objectContaining({ type: 'supplier', data: jasmine.objectContaining({ supplierName: 'Atlas', price: 10 }) }),
    );
  });

  it('ignores events it does not know', () => {
    controller.expectOne('/api/searches').flush('event: ping\ndata: {}\n\n' + STARTED);

    expect(received.map((event) => event.type)).toEqual(['started']);
  });

  it('stops the request when the subscriber leaves', () => {
    const request = controller.expectOne('/api/searches');

    subscription.unsubscribe();

    expect(request.cancelled).toBeTrue();
  });
});
