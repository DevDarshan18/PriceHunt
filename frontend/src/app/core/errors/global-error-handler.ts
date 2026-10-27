import { ErrorHandler, Injectable, inject } from '@angular/core';
import { LoggerService } from '../logging/logger.service';
import { NotificationService } from '../notifications/notification.service';
import { ApiError } from './api-error';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly log = inject(LoggerService).scoped('App');
  private readonly notifications = inject(NotificationService);

  handleError(error: unknown): void {
    if (error instanceof ApiError) {
      return;
    }

    this.log.error('Unhandled error', error);
    this.notifications.error('Something went wrong. Please try again.');
  }
}
