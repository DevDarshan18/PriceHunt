import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../testing/http-testing';
import { CORRELATION_ID_HEADER } from '../errors/api-error';
import { correlationIdInterceptor } from './correlation-id.interceptor';

describe('correlationIdInterceptor', () => {
  it('adds a different correlation id to every request', () => {
    configureHttp(correlationIdInterceptor);
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);

    http.get('/one').subscribe();
    http.get('/two').subscribe();

    const first = controller.expectOne('/one');
    const second = controller.expectOne('/two');
    expect(first.request.headers.get(CORRELATION_ID_HEADER)).toMatch(/^[0-9a-f-]{36}$/);
    expect(first.request.headers.get(CORRELATION_ID_HEADER)).not.toBe(second.request.headers.get(CORRELATION_ID_HEADER));
  });
});
