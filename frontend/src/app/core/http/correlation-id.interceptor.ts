import { HttpInterceptorFn } from '@angular/common/http';
import { CORRELATION_ID_HEADER } from '../errors/api-error';

export const correlationIdInterceptor: HttpInterceptorFn = (request, next) =>
  next(request.clone({ setHeaders: { [CORRELATION_ID_HEADER]: crypto.randomUUID() } }));
