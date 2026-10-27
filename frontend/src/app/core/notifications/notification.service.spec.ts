import { fakeAsync, tick } from '@angular/core/testing';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  it('adds each message with its own id', () => {
    const service = new NotificationService();

    service.error('First');
    service.error('Second');

    const items = service.items();
    expect(items.map((item) => item.message)).toEqual(['First', 'Second']);
    expect(items[0].id).not.toBe(items[1].id);
  });

  it('removes a message when it is dismissed', () => {
    const service = new NotificationService();
    service.error('First');
    service.error('Second');

    service.dismiss(service.items()[0].id);

    expect(service.items().map((item) => item.message)).toEqual(['Second']);
  });

  it('removes a message by itself after six seconds', fakeAsync(() => {
    const service = new NotificationService();
    service.error('Temporary');

    tick(5999);
    expect(service.items().length).toBe(1);

    tick(1);
    expect(service.items()).toEqual([]);
  }));
});
