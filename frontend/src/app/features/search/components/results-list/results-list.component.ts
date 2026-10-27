import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SecondsPipe } from '../../../../shared/pipes/seconds.pipe';
import { PriceCellComponent } from '../../../../shared/ui/price-cell/price-cell.component';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge/status-badge.component';
import { ResultRow } from '../../models/search.models';

@Component({
  selector: 'app-results-list',
  standalone: true,
  imports: [PriceCellComponent, StatusBadgeComponent, SecondsPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './results-list.component.html',
  styleUrl: './results-list.component.scss',
})
export class ResultsListComponent {
  readonly rows = input.required<ResultRow[]>();
}
