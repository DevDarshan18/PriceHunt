import { Injectable, InjectionToken, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { LOG_LEVEL_ORDER, LogLevel } from './log-level';

export const LOG_LEVEL = new InjectionToken<LogLevel>('LOG_LEVEL', {
  factory: () => environment.logLevel,
});

export interface ScopedLogger {
  debug(message: string, data?: unknown): void;
  info(message: string, data?: unknown): void;
  warn(message: string, data?: unknown): void;
  error(message: string, data?: unknown): void;
}

@Injectable({ providedIn: 'root' })
export class LoggerService {
  private readonly threshold = inject(LOG_LEVEL);

  scoped(context: string): ScopedLogger {
    return {
      debug: (message, data) => this.write('debug', context, message, data),
      info: (message, data) => this.write('info', context, message, data),
      warn: (message, data) => this.write('warn', context, message, data),
      error: (message, data) => this.write('error', context, message, data),
    };
  }

  private write(level: LogLevel, context: string, message: string, data?: unknown): void {
    if (LOG_LEVEL_ORDER[level] < LOG_LEVEL_ORDER[this.threshold]) {
      return;
    }

    const line = `${new Date().toISOString()} ${level.toUpperCase().padEnd(5)} [${context}] ${message}`;
    const extra = data === undefined ? [] : [data];
    console[level](line, ...extra);
  }
}
