import { Injectable, signal } from '@angular/core';

export interface Notification {
  id: number;
  message: string;
}

const DISPLAY_TIME_MS = 6000;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  private readonly _items = signal<Notification[]>([]);

  readonly items = this._items.asReadonly();

  error(message: string): void {
    const notification = { id: this.nextId++, message };
    this._items.update((items) => [...items, notification]);
    setTimeout(() => this.dismiss(notification.id), DISPLAY_TIME_MS);
  }

  dismiss(id: number): void {
    this._items.update((items) => items.filter((item) => item.id !== id));
  }
}
