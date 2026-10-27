import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../testing/http-testing';
import { CORRELATION_ID_HEADER } from '../errors/api-error';
import { LOG_LEVEL } from '../logging/logger.service';
import { correlationIdInterceptor } from './correlation-id.interceptor';
import { requestLoggingInterceptor } from './request-logging.interceptor';

describe('requestLoggingInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    configureHttp(correlationIdInterceptor, requestLoggingInterceptor);
    TestBed.overrideProvider(LOG_LEVEL, { useValue: 'debug' });
    spyOn(console, 'debug');
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  it('logs the request and then the response with how long it took', () => {
    http.get('/api/suppliers').subscribe();
    controller.expectOne('/api/suppliers').flush([]);

    const lines = (console.debug as jasmine.Spy).calls.allArgs().map(([line]) => line as string);

    expect(lines.length).toBe(2);
    expect(lines[0]).toContain('[Http] GET /api/suppliers');
    expect(lines[1]).toMatch(/\[Http\] GET \/api\/suppliers -> 200 in \d+ ms/);
  });

  it('includes the correlation id so the line can be matched with the server log', () => {
    http.get('/api/suppliers').subscribe();
    const request = controller.expectOne('/api/suppliers');
    request.flush([]);

    const correlationId = request.request.headers.get(CORRELATION_ID_HEADER);
    const details = (console.debug as jasmine.Spy).calls.allArgs().map(([, data]) => data);

    expect(correlationId).toBeTruthy();
    expect(details).toEqual([{ correlationId }, { correlationId }]);
  });

  it('does not log a response line for a request that never completes', () => {
    http.get('/api/suppliers').subscribe();

    expect((console.debug as jasmine.Spy).calls.count()).toBe(1);
    controller.expectOne('/api/suppliers');
  });
});
