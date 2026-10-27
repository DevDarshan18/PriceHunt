import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge/status-badge.component';
import { FinalSearchStatus } from '../../models/search.models';
import { SearchPhase } from '../../state/search.store';

@Component({
  selector: 'app-search-progress',
  standalone: true,
  imports: [StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-progress.component.html',
  styleUrl: './search-progress.component.scss',
})
export class SearchProgressComponent {
  readonly phase = input.required<SearchPhase>();
  readonly responded = input.required<number>();
  readonly selected = input.required<number>();
  readonly finalStatus = input<FinalSearchStatus | null>(null);

  protected readonly statusLabel = computed(() => {
    if (this.phase() === 'failed') {
      return 'Failed';
    }

    return this.finalStatus() ?? 'Running';
  });
}
