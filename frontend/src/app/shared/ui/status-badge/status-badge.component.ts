import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

type Tone = 'success' | 'warning' | 'danger' | 'neutral';

const STATUSES: Record<string, { label: string; tone: Tone }> = {
  Completed: { label: 'Completed', tone: 'success' },
  Succeeded: { label: 'Succeeded', tone: 'success' },
  TimedOut: { label: 'Timed out', tone: 'warning' },
  Cancelled: { label: 'Cancelled', tone: 'warning' },
  Failed: { label: 'Failed', tone: 'danger' },
  Running: { label: 'Searching', tone: 'neutral' },
  Waiting: { label: 'Waiting', tone: 'neutral' },
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss',
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  protected readonly view = computed(() => STATUSES[this.status()] ?? { label: this.status(), tone: 'neutral' as Tone });
}
