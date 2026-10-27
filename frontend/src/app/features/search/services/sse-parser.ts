export interface ServerSentEvent {
  name: string;
  data: string;
}

export interface ParsedEvents {
  events: ServerSentEvent[];
  consumed: number;
}

const EVENT_SEPARATOR = '\n\n';

export function parseServerSentEvents(text: string): ParsedEvents {
  const events: ServerSentEvent[] = [];
  let consumed = 0;
  let end = text.indexOf(EVENT_SEPARATOR);

  while (end !== -1) {
    const event = parseBlock(text.slice(consumed, end));
    if (event) {
      events.push(event);
    }

    consumed = end + EVENT_SEPARATOR.length;
    end = text.indexOf(EVENT_SEPARATOR, consumed);
  }

  return { events, consumed };
}

function parseBlock(block: string): ServerSentEvent | null {
  let name = 'message';
  const data: string[] = [];

  for (const line of block.split('\n')) {
    if (line.startsWith('event:')) {
      name = line.slice('event:'.length).trim();
    } else if (line.startsWith('data:')) {
      data.push(line.slice('data:'.length).trim());
    }
  }

  return data.length > 0 ? { name, data: data.join('\n') } : null;
}
