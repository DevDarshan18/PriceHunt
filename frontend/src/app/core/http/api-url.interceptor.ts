import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from '../config/api-base-url.token';

export const apiUrlInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/')) {
    return next(request);
  }

  return next(request.clone({ url: inject(API_BASE_URL) + request.url }));
};
