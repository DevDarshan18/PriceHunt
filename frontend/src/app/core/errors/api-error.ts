import { HttpErrorResponse } from '@angular/common/http';

export const CORRELATION_ID_HEADER = 'X-Correlation-Id';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly errors: Record<string, string[]> = {},
    readonly traceId?: string,
  ) {
    super(message);
  }

  get isValidation(): boolean {
    return this.status === 400 && Object.keys(this.errors).length > 0;
  }

  get isUnexpected(): boolean {
    return this.status === 0 || this.status >= 500;
  }

  static from(error: unknown): ApiError {
    if (error instanceof ApiError) {
      return error;
    }

    if (!(error instanceof HttpErrorResponse)) {
      return new ApiError(0, error instanceof Error ? error.message : 'Something went wrong.');
    }

    if (error.status === 0) {
      return new ApiError(0, "The server can't be reached. Check that the API is running.");
    }

    const problem = readProblemDetails(error.error);
    const message = problem.detail ?? problem.title ?? error.statusText;
    return new ApiError(error.status, message, problem.errors, problem.traceId ?? error.headers.get(CORRELATION_ID_HEADER) ?? undefined);
  }
}

function readProblemDetails(body: unknown): ProblemDetails {
  if (typeof body === 'string') {
    try {
      return JSON.parse(body) as ProblemDetails;
    } catch {
      return {};
    }
  }

  return (body ?? {}) as ProblemDetails;
}
