import { parseServerSentEvents } from './sse-parser';

describe('parseServerSentEvents', () => {
  it('reads complete events', () => {
    const text = 'event: started\ndata: {"a":1}\n\nevent: supplier\ndata: {"b":2}\n\n';

    const { events, consumed } = parseServerSentEvents(text);

    expect(events).toEqual([
      { name: 'started', data: '{"a":1}' },
      { name: 'supplier', data: '{"b":2}' },
    ]);
    expect(consumed).toBe(text.length);
  });

  it('leaves an unfinished event for the next chunk', () => {
    const complete = 'event: started\ndata: {}\n\n';
    const partial = 'event: supplier\ndata: {"b"';

    const { events, consumed } = parseServerSentEvents(complete + partial);

    expect(events.length).toBe(1);
    expect(consumed).toBe(complete.length);
  });

  it('returns nothing for an empty or unfinished text', () => {
    expect(parseServerSentEvents('')).toEqual({ events: [], consumed: 0 });
    expect(parseServerSentEvents('event: x\ndata: 1')).toEqual({ events: [], consumed: 0 });
  });

  it('defaults the name to message and skips blocks without data', () => {
    const { events } = parseServerSentEvents('data: hello\n\n: just a comment\n\n');

    expect(events).toEqual([{ name: 'message', data: 'hello' }]);
  });
});
