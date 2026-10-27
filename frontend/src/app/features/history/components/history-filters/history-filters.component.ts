import { Component, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ApiError } from '../../../../core/errors/api-error';
import { showServerErrors } from '../../../../shared/forms/server-errors';
import { LocationsService } from '../../../../shared/data-access/locations.service';
import { notBefore, revalidateWhenChanged } from '../../../../shared/forms/validators';
import { Supplier } from '../../../../shared/models/supplier.model';
import { AutocompleteComponent } from '../../../../shared/ui/autocomplete/autocomplete.component';
import { FieldErrorComponent } from '../../../../shared/ui/field-error/field-error.component';
import { SupplierPickerComponent } from '../../../../shared/ui/supplier-picker/supplier-picker.component';
import { HistoryFilters } from '../../models/history.models';

@Component({
  selector: 'app-history-filters',
  standalone: true,
  imports: [ReactiveFormsModule, AutocompleteComponent, SupplierPickerComponent, FieldErrorComponent],
  templateUrl: './history-filters.component.html',
})
export class HistoryFiltersComponent {
  readonly suppliers = input.required<Supplier[]>();
  readonly serverError = input<ApiError | null>(null);

  readonly filtersApplied = output<HistoryFilters>();

  protected readonly findLocations = inject(LocationsService).find;

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly form = this.formBuilder.group({
    from: [''],
    to: ['', [notBefore('from')]],
    supplierIds: [[] as string[]],
    origin: [''],
    destination: [''],
  });

  constructor() {
    revalidateWhenChanged(this.form.controls.from, this.form.controls.to);
    showServerErrors(this.form, this.serverError);
  }

  protected apply(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.filtersApplied.emit(this.form.getRawValue());
  }

  protected reset(): void {
    this.form.reset();
    this.apply();
  }
}
