import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCATIONS, placesLoader } from '../../../../../testing/search-fixtures';
import { LocationsService } from '../../../../shared/data-access/locations.service';
import { buttonLabelled, typeInto } from '../../../../../testing/dom-helpers';
import { ApiError } from '../../../../core/errors/api-error';
import { HistoryFilters } from '../../models/history.models';
import { HistoryFiltersComponent } from './history-filters.component';

describe('HistoryFiltersComponent', () => {
  let fixture: ComponentFixture<HistoryFiltersComponent>;
  let applied: HistoryFilters[];

  const element = () => fixture.nativeElement as HTMLElement;
  const input = (id: string) => element().querySelector<HTMLInputElement>(`#${id}`)!;
  const button = (label: string) => buttonLabelled(element(), label);
  const type = (id: string, value: string) => typeInto(element(), `#${id}`, value);

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [{ provide: LocationsService, useValue: { find: placesLoader(LOCATIONS) } }] });
    fixture = TestBed.createComponent(HistoryFiltersComponent);
    fixture.componentRef.setInput('suppliers', [{ id: 'a', name: 'Atlas' }]);
    applied = [];
    fixture.componentInstance.filtersApplied.subscribe((filters) => applied.push(filters));
    fixture.detectChanges();
  });

  it('starts with no filters and shows all suppliers', () => {
    expect(element().querySelector('.picker__summary')?.textContent).toBe('All suppliers');

    button('Apply').click();

    expect(applied).toEqual([{ from: '', to: '', supplierIds: [], origin: '', destination: '' }]);
  });

  it('applies the typed dates, locations and selected suppliers', () => {
    type('from', '2026-01-01');
    type('to', '2026-01-31');
    type('origin', 'Oslo');
    element().querySelector<HTMLButtonElement>('.picker__trigger')!.click();
    fixture.detectChanges();
    element().querySelector<HTMLInputElement>('input[type=checkbox]')!.click();

    button('Apply').click();

    expect(applied[0]).toEqual({ from: '2026-01-01', to: '2026-01-31', supplierIds: ['a'], origin: 'Oslo', destination: '' });
  });

  it('does not apply a place that is not in the list', () => {
    type('origin', 'Atlantis');
    button('Apply').click();
    fixture.detectChanges();

    expect(applied).toEqual([]);
    expect(element().textContent).toContain('Choose a place from the list.');
  });

  it('does not apply an end date before the start date', () => {
    type('from', '2026-02-01');
    type('to', '2026-01-01');
    button('Apply').click();
    fixture.detectChanges();

    expect(applied).toEqual([]);
    expect(element().textContent).toContain("To date can't be before the from date.");
  });

  it('clears the filters and searches again when reset', () => {
    type('origin', 'Oslo');
    fixture.detectChanges();

    button('Reset').click();
    fixture.detectChanges();

    expect(applied).toEqual([{ from: '', to: '', supplierIds: [], origin: '', destination: '' }]);
    expect(input('origin').value).toBe('');
  });

  it('shows a validation message from the server', () => {
    fixture.componentRef.setInput('serverError', new ApiError(400, 'Invalid', { to: ['End date is wrong.'] }));
    fixture.detectChanges();

    expect(element().textContent).toContain('End date is wrong.');
  });
});
