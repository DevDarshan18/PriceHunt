import { TestBed } from '@angular/core/testing';
import { LOG_LEVEL, LoggerService } from './logger.service';
import { LogLevel } from './log-level';

describe('LoggerService', () => {
  function createLogger(level: LogLevel) {
    TestBed.configureTestingModule({ providers: [{ provide: LOG_LEVEL, useValue: level }] });
    return TestBed.inject(LoggerService).scoped('Checkout');
  }

  beforeEach(() => {
    spyOn(console, 'debug');
    spyOn(console, 'info');
    spyOn(console, 'warn');
    spyOn(console, 'error');
  });

  it('writes the level, the context and the message', () => {
    createLogger('debug').info('Saved');

    expect(console.info).toHaveBeenCalledWith(jasmine.stringMatching(/INFO\s+\[Checkout\] Saved$/));
  });

  it('skips messages below the configured level', () => {
    const log = createLogger('warn');

    log.debug('noise');
    log.info('noise');
    log.warn('careful');
    log.error('broken');

    expect(console.debug).not.toHaveBeenCalled();
    expect(console.info).not.toHaveBeenCalled();
    expect(console.warn).toHaveBeenCalledTimes(1);
    expect(console.error).toHaveBeenCalledTimes(1);
  });

  it('passes extra data to the console only when it is given', () => {
    const log = createLogger('debug');

    log.debug('with data', { id: 1 });
    log.debug('without data');

    expect(console.debug).toHaveBeenCalledWith(jasmine.any(String), { id: 1 });
    expect((console.debug as jasmine.Spy).calls.mostRecent().args.length).toBe(1);
  });
});
