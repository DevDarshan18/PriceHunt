import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../testing/http-testing';
import { API_BASE_URL } from '../config/api-base-url.token';
import { apiUrlInterceptor } from './api-url.interceptor';

describe('apiUrlInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    configureHttp(apiUrlInterceptor);
    TestBed.overrideProvider(API_BASE_URL, { useValue: 'http://api.test' });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  it('prefixes relative urls with the api base url', () => {
    http.get('/api/suppliers').subscribe();

    const request = controller.expectOne('http://api.test/api/suppliers');

    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('leaves absolute urls alone', () => {
    http.get('https://other.test/data').subscribe();

    const request = controller.expectOne('https://other.test/data');

    expect(request.request.url).toBe('https://other.test/data');
    request.flush({});
  });
});
