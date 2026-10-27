import { Component, inject } from '@angular/core';
import { SuppliersService } from '../../shared/data-access/suppliers.service';
import { ErrorMessageComponent } from '../../shared/ui/error-message/error-message.component';
import { PagerComponent } from '../../shared/ui/pager/pager.component';
import { HistoryFiltersComponent } from './components/history-filters/history-filters.component';
import { HistoryTableComponent } from './components/history-table/history-table.component';
import { HistoryStore } from './state/history.store';

@Component({
  selector: 'app-history-page',
  standalone: true,
  imports: [HistoryFiltersComponent, HistoryTableComponent, PagerComponent, ErrorMessageComponent],
  providers: [HistoryStore],
  templateUrl: './history-page.component.html',
})
export class HistoryPageComponent {
  protected readonly store = inject(HistoryStore);
  protected readonly suppliers = inject(SuppliersService).suppliers;
}
