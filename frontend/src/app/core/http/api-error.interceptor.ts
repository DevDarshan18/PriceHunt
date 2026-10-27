import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiError, CORRELATION_ID_HEADER } from '../errors/api-error';
import { LoggerService } from '../logging/logger.service';
import { NotificationService } from '../notifications/notification.service';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const log = inject(LoggerService).scoped('Http');
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      const apiError = ApiError.from(error);
      const details = {
        status: apiError.status,
        traceId: apiError.traceId,
        correlationId: request.headers.get(CORRELATION_ID_HEADER),
      };

      if (apiError.isUnexpected) {
        log.error(`${request.method} ${request.urlWithParams} failed: ${apiError.message}`, details);
        notifications.error(apiError.message);
      } else {
        log.warn(`${request.method} ${request.urlWithParams} was rejected: ${apiError.message}`, details);
      }

      return throwError(() => apiError);
    }),
  );
};
