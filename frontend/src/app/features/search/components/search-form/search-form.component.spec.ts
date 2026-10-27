import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCATIONS, placesLoader } from '../../../../../testing/search-fixtures';
import { LocationsService } from '../../../../shared/data-access/locations.service';
import { buttonLabelled, typeInto } from '../../../../../testing/dom-helpers';
import { ApiError } from '../../../../core/errors/api-error';
import { Supplier } from '../../../../shared/models/supplier.model';
import { addDays, today } from '../../../../shared/utils/date.utils';
import { SearchRequest } from '../../models/search.models';
import { SearchFormComponent } from './search-form.component';

const SUPPLIERS: Supplier[] = [
  { id: 'a', name: 'Atlas' },
  { id: 'b', name: 'BlueWave' },
];

describe('SearchFormComponent', () => {
  let fixture: ComponentFixture<SearchFormComponent>;
  let requests: SearchRequest[];

  const element = () => fixture.nativeElement as HTMLElement;
  const input = (id: string) => element().querySelector<HTMLInputElement>(`#${id}`)!;
  const errors = () => Array.from(element().querySelectorAll('app-field-error')).map((node) => node.textContent?.trim()).filter(Boolean);

  const type = (id: string, value: string) => typeInto(element(), `#${id}`, value);

  function submit(): void {
    element().querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [{ provide: LocationsService, useValue: { find: placesLoader(LOCATIONS) } }] });
    fixture = TestBed.createComponent(SearchFormComponent);
    fixture.componentRef.setInput('suppliers', SUPPLIERS);
    requests = [];
    fixture.componentInstance.searchRequested.subscribe((request) => requests.push(request));
    fixture.detectChanges();
  });

  it('selects every supplier and starts with a week of dates', () => {
    fixture.detectChanges();

    expect(element().querySelector('.picker__summary')?.textContent).toBe('All suppliers');
    expect(input('fromDate').value).toBe(today());
    expect(input('toDate').value).toBe(addDays(today(), 7));
  });

  function openWith(previous: SearchRequest): HTMLElement {
    const reopened = TestBed.createComponent(SearchFormComponent);
    reopened.componentRef.setInput('suppliers', SUPPLIERS);
    reopened.componentRef.setInput('initial', previous);
    reopened.detectChanges();
    return reopened.nativeElement as HTMLElement;
  }

  it('starts from the previous search when it is given one', () => {
    const root = openWith({
      fromLocation: 'Oslo',
      toLocation: 'Bergen',
      fromDate: addDays(today(), 3),
      toDate: addDays(today(), 9),
      supplierIds: ['b'],
    });

    expect(root.querySelector<HTMLInputElement>('#fromLocation')!.value).toBe('Oslo');
    expect(root.querySelector<HTMLInputElement>('#toLocation')!.value).toBe('Bergen');
    expect(root.querySelector<HTMLInputElement>('#fromDate')!.value).toBe(addDays(today(), 3));
    expect(root.querySelector<HTMLInputElement>('#toDate')!.value).toBe(addDays(today(), 9));
    expect(root.querySelector('.picker__summary')?.textContent).not.toBe('All suppliers');
  });

  it('keeps the places but uses fresh dates when the previous dates have passed', () => {
    const root = openWith({
      fromLocation: 'Oslo',
      toLocation: 'Bergen',
      fromDate: addDays(today(), -5),
      toDate: addDays(today(), -1),
      supplierIds: ['a', 'b'],
    });

    expect(root.querySelector<HTMLInputElement>('#fromLocation')!.value).toBe('Oslo');
    expect(root.querySelector<HTMLInputElement>('#fromDate')!.value).toBe(today());
    expect(root.querySelector<HTMLInputElement>('#toDate')!.value).toBe(addDays(today(), 7));
  });

  it('does not search and shows what is missing when the locations are empty', () => {
    submit();

    expect(requests).toEqual([]);
    expect(errors()).toContain('Origin is required.');
    expect(errors()).toContain('Destination is required.');
  });

  it('only accepts a place from the list', () => {
    type('fromLocation', 'Atlantis');
    type('toLocation', 'Hamburg');
    submit();

    expect(requests).toEqual([]);
    expect(errors()).toContain('Choose a place from the list.');
  });

  it('accepts a place from the list however it was typed', () => {
    type('fromLocation', ' rotterdam ');
    type('toLocation', 'HAMBURG');
    submit();

    expect(requests.length).toBe(1);
  });

  it('rejects the same place for both locations', () => {
    type('fromLocation', 'Oslo');
    type('toLocation', ' oslo');
    submit();

    expect(requests).toEqual([]);
    expect(errors()).toContain("Destination can't be the same as the origin.");
  });

  it('rejects an end date before the start date, even when the start date is changed last', () => {
    type('fromLocation', 'Oslo');
    type('toLocation', 'Bergen');
    type('toDate', addDays(today(), 2));
    type('fromDate', addDays(today(), 5));
    submit();

    expect(requests).toEqual([]);
    expect(errors()).toContain("To date can't be before the from date.");
  });

  it('asks for a search with the typed values and the selected suppliers', () => {
    type('fromLocation', 'Rotterdam');
    type('toLocation', 'Hamburg');
    submit();

    expect(requests).toEqual([
      {
        fromLocation: 'Rotterdam',
        toLocation: 'Hamburg',
        fromDate: today(),
        toDate: addDays(today(), 7),
        supplierIds: ['a', 'b'],
      },
    ]);
  });

  it('shows a validation message from the server on the matching field', () => {
    fixture.componentRef.setInput('serverError', new ApiError(400, 'Invalid', { toLocation: ['Server says no.'] }));
    fixture.detectChanges();

    expect(errors()).toContain('Server says no.');
  });

  it('lets the user cancel only while a search is running', () => {
    const cancelButton = () => buttonLabelled(element(), 'Cancel');
    let cancellations = 0;
    fixture.componentInstance.cancelRequested.subscribe(() => cancellations++);

    expect(cancelButton().disabled).toBeTrue();

    fixture.componentRef.setInput('busy', true);
    fixture.detectChanges();
    cancelButton().click();

    expect(cancelButton().disabled).toBeFalse();
    expect(cancellations).toBe(1);
  });
});
