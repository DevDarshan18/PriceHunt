import { TestBed } from '@angular/core/testing';
import { LOG_LEVEL } from '../logging/logger.service';
import { NotificationService } from '../notifications/notification.service';
import { ApiError } from './api-error';
import { GlobalErrorHandler } from './global-error-handler';

describe('GlobalErrorHandler', () => {
  let handler: GlobalErrorHandler;
  let notifications: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [GlobalErrorHandler, { provide: LOG_LEVEL, useValue: 'error' }],
    });
    spyOn(console, 'error');
    handler = TestBed.inject(GlobalErrorHandler);
    notifications = TestBed.inject(NotificationService);
  });

  it('logs an unexpected error and tells the user something went wrong', () => {
    const error = new Error('boom');

    handler.handleError(error);

    expect(console.error).toHaveBeenCalledWith(jasmine.stringMatching(/\[App\] Unhandled error/), error);
    expect(notifications.items().map((item) => item.message)).toEqual(['Something went wrong. Please try again.']);
  });

  it('leaves an ApiError alone because the http interceptor has already reported it', () => {
    handler.handleError(new ApiError(500, 'Server down'));

    expect(console.error).not.toHaveBeenCalled();
    expect(notifications.items()).toEqual([]);
  });
});
