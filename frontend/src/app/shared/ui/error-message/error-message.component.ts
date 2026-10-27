import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ApiError } from '../../../core/errors/api-error';

@Component({
  selector: 'app-error-message',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './error-message.component.html',
  styleUrl: './error-message.component.scss',
})
export class ErrorMessageComponent {
  readonly error = input.required<ApiError | null>();
}
