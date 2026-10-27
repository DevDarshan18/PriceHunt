import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SecondsPipe } from '../../../../shared/pipes/seconds.pipe';
import { StatusBadgeComponent } from '../../../../shared/ui/status-badge/status-badge.component';
import { HistoryItem, HistorySortField } from '../../models/history.models';

interface Column {
  field: HistorySortField;
  label: string;
}

const COLUMNS: Column[] = [
  { field: 'date', label: 'Date' },
  { field: 'route', label: 'Route' },
  { field: 'supplier', label: 'Supplier' },
  { field: 'status', label: 'Status' },
  { field: 'price', label: 'Price' },
  { field: 'responseTime', label: 'Response time' },
];

@Component({
  selector: 'app-history-table',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, SecondsPipe, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './history-table.component.html',
  styleUrl: './history-table.component.scss',
})
export class HistoryTableComponent {
  readonly items = input.required<HistoryItem[]>();
  readonly sortBy = input.required<HistorySortField>();
  readonly descending = input.required<boolean>();
  readonly loading = input(false);

  readonly sortChanged = output<HistorySortField>();

  protected readonly columns = COLUMNS;

  protected ariaSort(field: HistorySortField): 'ascending' | 'descending' | 'none' {
    if (field !== this.sortBy()) {
      return 'none';
    }

    return this.descending() ? 'descending' : 'ascending';
  }
}
