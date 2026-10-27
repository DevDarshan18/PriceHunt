import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { ApiError, CORRELATION_ID_HEADER } from './api-error';

describe('ApiError.from', () => {
  it('reads a validation problem with its field errors', () => {
    const error = ApiError.from(
      new HttpErrorResponse({
        status: 400,
        error: { title: 'Invalid', detail: 'See errors.', errors: { fromDate: ['Too early.'] }, traceId: 'trace-1' },
      }),
    );

    expect(error.status).toBe(400);
    expect(error.message).toBe('See errors.');
    expect(error.errors).toEqual({ fromDate: ['Too early.'] });
    expect(error.traceId).toBe('trace-1');
    expect(error.isValidation).toBeTrue();
    expect(error.isUnexpected).toBeFalse();
  });

  it('parses a problem that arrived as text, as it does for the streaming request', () => {
    const body = JSON.stringify({ title: 'Invalid', errors: { toLocation: ['Same place.'] } });

    const error = ApiError.from(new HttpErrorResponse({ status: 400, error: body }));

    expect(error.errors['toLocation']).toEqual(['Same place.']);
  });

  it('treats a server error as unexpected and falls back to the correlation id header', () => {
    const headers = new HttpHeaders({ [CORRELATION_ID_HEADER]: 'abc' });

    const error = ApiError.from(new HttpErrorResponse({ status: 500, statusText: 'Server Error', headers, error: {} }));

    expect(error.isUnexpected).toBeTrue();
    expect(error.message).toBe('Server Error');
    expect(error.traceId).toBe('abc');
  });

  it('explains that the server cannot be reached when there is no response', () => {
    const error = ApiError.from(new HttpErrorResponse({ status: 0 }));

    expect(error.isUnexpected).toBeTrue();
    expect(error.message).toContain("can't be reached");
  });

  it('wraps other errors and keeps an ApiError as it is', () => {
    const existing = new ApiError(404, 'Missing');

    expect(ApiError.from(new Error('boom')).message).toBe('boom');
    expect(ApiError.from(existing)).toBe(existing);
  });
});
