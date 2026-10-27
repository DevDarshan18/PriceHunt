import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, ErrorHandler } from '@angular/core';
import { provideRouter } from '@angular/router';
import { GlobalErrorHandler } from './core/errors/global-error-handler';
import { apiErrorInterceptor } from './core/http/api-error.interceptor';
import { apiUrlInterceptor } from './core/http/api-url.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { requestLoggingInterceptor } from './core/http/request-logging.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([apiUrlInterceptor, correlationIdInterceptor, requestLoggingInterceptor, apiErrorInterceptor]),
    ),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
  ],
};
