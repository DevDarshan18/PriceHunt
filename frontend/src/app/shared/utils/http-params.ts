import { HttpParams } from '@angular/common/http';

type ParamValue = string | number | boolean | null | undefined | readonly (string | number)[];

export function toHttpParams(values: Record<string, ParamValue>): HttpParams {
  let params = new HttpParams();

  for (const [key, value] of Object.entries(values)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    const list = Array.isArray(value) ? value : [value];
    for (const item of list) {
      params = params.append(key, String(item));
    }
  }

  return params;
}
