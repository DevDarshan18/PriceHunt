import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-toast-outlet',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toast-outlet.component.html',
  styleUrl: './toast-outlet.component.scss',
})
export class ToastOutletComponent {
  protected readonly notifications = inject(NotificationService);
}
