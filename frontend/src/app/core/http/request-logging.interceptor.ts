import { HttpEventType, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { CORRELATION_ID_HEADER } from '../errors/api-error';
import { LoggerService } from '../logging/logger.service';

export const requestLoggingInterceptor: HttpInterceptorFn = (request, next) => {
  const log = inject(LoggerService).scoped('Http');
  const correlationId = request.headers.get(CORRELATION_ID_HEADER);
  const startedAt = performance.now();

  log.debug(`${request.method} ${request.urlWithParams}`, { correlationId });

  return next(request).pipe(
    tap((event) => {
      if (event.type === HttpEventType.Response) {
        const elapsed = Math.round(performance.now() - startedAt);
        log.debug(`${request.method} ${request.urlWithParams} -> ${event.status} in ${elapsed} ms`, { correlationId });
      }
    }),
  );
};
