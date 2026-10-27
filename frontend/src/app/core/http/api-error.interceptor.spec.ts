import { HttpClient } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../testing/http-testing';
import { ApiError } from '../errors/api-error';
import { LOG_LEVEL } from '../logging/logger.service';
import { NotificationService } from '../notifications/notification.service';
import { apiErrorInterceptor } from './api-error.interceptor';

describe('apiErrorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let notifications: NotificationService;

  beforeEach(() => {
    configureHttp(apiErrorInterceptor);
    TestBed.overrideProvider(LOG_LEVEL, { useValue: 'warn' });
    spyOn(console, 'error');
    spyOn(console, 'warn');
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    notifications = TestBed.inject(NotificationService);
  });

  function failWith(status: number, body: object): ApiError {
    let received: unknown;
    http.get('/api/x').subscribe({ error: (error) => (received = error) });
    controller.expectOne('/api/x').flush(body, { status, statusText: 'Failed' });
    return received as ApiError;
  }

  it('turns a failed request into an ApiError', () => {
    const error = failWith(400, { title: 'Invalid', errors: { page: ['Too low.'] } });

    expect(error).toBeInstanceOf(ApiError);
    expect(error.isValidation).toBeTrue();
  });

  it('does not show a notification for a validation error because the form shows it', () => {
    failWith(400, { errors: { page: ['Too low.'] } });

    expect(notifications.items().length).toBe(0);
    expect(console.warn).toHaveBeenCalled();
  });

  it('shows a notification and logs an error when the server fails', () => {
    failWith(500, { title: 'Unexpected', detail: 'Quote the trace id.' });

    expect(notifications.items().map((item) => item.message)).toEqual(['Quote the trace id.']);
    expect(console.error).toHaveBeenCalled();
  });
});
