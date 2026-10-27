import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LOG_LEVEL } from '../app/core/logging/logger.service';

export function configureHttp(...interceptors: HttpInterceptorFn[]): void {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors(interceptors)),
      provideHttpClientTesting(),
      { provide: LOG_LEVEL, useValue: 'error' },
    ],
  });
}
