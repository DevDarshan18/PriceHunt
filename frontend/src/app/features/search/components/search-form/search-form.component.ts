import { Component, OnInit, effect, inject, input, output, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiError } from '../../../../core/errors/api-error';
import { LocationsService } from '../../../../shared/data-access/locations.service';
import { SEARCH_RULES } from '../../../../shared/forms/search-rules';
import { showServerErrors } from '../../../../shared/forms/server-errors';
import {
  differentFrom,
  minSelected,
  notBefore,
  notInPast,
  revalidateWhenChanged,
  withinDaysOf,
} from '../../../../shared/forms/validators';
import { Supplier } from '../../../../shared/models/supplier.model';
import { AutocompleteComponent } from '../../../../shared/ui/autocomplete/autocomplete.component';
import { FieldErrorComponent } from '../../../../shared/ui/field-error/field-error.component';
import { SupplierPickerComponent } from '../../../../shared/ui/supplier-picker/supplier-picker.component';
import { addDays, today } from '../../../../shared/utils/date.utils';
import { SearchRequest } from '../../models/search.models';

@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [ReactiveFormsModule, AutocompleteComponent, SupplierPickerComponent, FieldErrorComponent],
  templateUrl: './search-form.component.html',
})
export class SearchFormComponent implements OnInit {
  readonly suppliers = input.required<Supplier[]>();
  readonly initial = input<SearchRequest | null>(null);
  readonly busy = input(false);
  readonly serverError = input<ApiError | null>(null);

  readonly searchRequested = output<SearchRequest>();
  readonly cancelRequested = output<void>();

  protected readonly findLocations = inject(LocationsService).find;
  protected readonly minDate = today();

  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly form = this.formBuilder.group({
    fromLocation: ['', [Validators.required]],
    toLocation: ['', [Validators.required, differentFrom('fromLocation')]],
    fromDate: [today(), [Validators.required, notInPast()]],
    toDate: [addDays(today(), 7), [Validators.required, notBefore('fromDate'), withinDaysOf('fromDate', SEARCH_RULES.maxRangeDays)]],
    supplierIds: [[] as string[], [minSelected()]],
  });

  constructor() {
    revalidateWhenChanged(this.form.controls.fromLocation, this.form.controls.toLocation);
    revalidateWhenChanged(this.form.controls.fromDate, this.form.controls.toDate);

    effect(() => {
      const allSupplierIds = this.suppliers().map((supplier) => supplier.id);
      untracked(() => this.form.controls.supplierIds.setValue(this.initial()?.supplierIds ?? allSupplierIds));
    });

    showServerErrors(this.form, this.serverError);
  }

  ngOnInit(): void {
    const initial = this.initial();
    if (!initial) {
      return;
    }

    const { fromLocation, toLocation } = initial;
    this.form.patchValue(initial.fromDate >= today() ? initial : { fromLocation, toLocation });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.searchRequested.emit(this.form.getRawValue());
  }
}
