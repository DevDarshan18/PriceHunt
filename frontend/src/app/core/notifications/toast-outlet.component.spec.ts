import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationService } from './notification.service';
import { ToastOutletComponent } from './toast-outlet.component';

describe('ToastOutletComponent', () => {
  let fixture: ComponentFixture<ToastOutletComponent>;
  let notifications: NotificationService;

  const toasts = () => Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('.toast'));

  beforeEach(() => {
    fixture = TestBed.createComponent(ToastOutletComponent);
    notifications = TestBed.inject(NotificationService);
    fixture.detectChanges();
  });

  it('shows nothing when there are no notifications', () => {
    expect(toasts()).toEqual([]);
  });

  it('shows each notification as an alert', () => {
    notifications.error('Server down');
    notifications.error('Try again');
    fixture.detectChanges();

    expect(toasts().map((toast) => toast.textContent?.replace('×', '').trim())).toEqual(['Server down', 'Try again']);
    expect(toasts().every((toast) => toast.getAttribute('role') === 'alert')).toBeTrue();
  });

  it('removes a notification when its close button is clicked', () => {
    notifications.error('Server down');
    fixture.detectChanges();

    toasts()[0].querySelector<HTMLButtonElement>('.toast__close')!.click();
    fixture.detectChanges();

    expect(toasts()).toEqual([]);
  });
});
