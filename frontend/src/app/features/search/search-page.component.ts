import { Component, inject } from '@angular/core';
import { SuppliersService } from '../../shared/data-access/suppliers.service';
import { ErrorMessageComponent } from '../../shared/ui/error-message/error-message.component';
import { ResultsListComponent } from './components/results-list/results-list.component';
import { SearchFormComponent } from './components/search-form/search-form.component';
import { SearchProgressComponent } from './components/search-progress/search-progress.component';
import { SearchStore } from './state/search.store';

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [SearchFormComponent, SearchProgressComponent, ResultsListComponent, ErrorMessageComponent],
  templateUrl: './search-page.component.html',
})
export class SearchPageComponent {
  protected readonly store = inject(SearchStore);
  protected readonly suppliers = inject(SuppliersService).suppliers;
}
