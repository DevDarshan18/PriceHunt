import { HttpClient, HttpDownloadProgressEvent, HttpEvent, HttpEventType } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, concatMap, defer, filter, map } from 'rxjs';
import { SearchEvent, SearchRequest } from '../models/search.models';
import { ServerSentEvent, parseServerSentEvents } from './sse-parser';

const EVENT_TYPES = ['started', 'supplier', 'finished'];

@Injectable({ providedIn: 'root' })
export class SearchStreamService {
  private readonly http = inject(HttpClient);

  search(request: SearchRequest): Observable<SearchEvent> {
    return defer(() => {
      let consumed = 0;

      return this.http
        .post('/api/searches', request, { observe: 'events', responseType: 'text', reportProgress: true })
        .pipe(
          map(textReceivedSoFar),
          filter((text): text is string => text !== null),
          concatMap((text) => {
            const parsed = parseServerSentEvents(text.slice(consumed));
            consumed += parsed.consumed;
            return parsed.events.filter((event) => EVENT_TYPES.includes(event.name)).map(toSearchEvent);
          }),
        );
    });
  }
}

function textReceivedSoFar(event: HttpEvent<string>): string | null {
  switch (event.type) {
    case HttpEventType.DownloadProgress:
      return (event as HttpDownloadProgressEvent).partialText ?? null;
    case HttpEventType.Response:
      return event.body;
    default:
      return null;
  }
}

function toSearchEvent(event: ServerSentEvent): SearchEvent {
  return { type: event.name, data: JSON.parse(event.data) } as SearchEvent;
}
