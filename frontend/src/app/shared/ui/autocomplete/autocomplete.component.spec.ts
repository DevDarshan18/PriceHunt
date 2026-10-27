import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, of, throwError } from 'rxjs';
import { placesLoader } from '../../../../testing/search-fixtures';
import { PagedResult } from '../../models/paged-result.model';
import { AutocompleteComponent, OptionLoader } from './autocomplete.component';

const PLACES = ['Amsterdam', 'Antwerp', 'Bergen', 'Berlin', 'Hamburg', 'Lisbon', 'New York', 'Oslo', 'Porto', 'Rotterdam', 'Tallinn', 'Vigo'];
const PAGE_SIZE = 5;

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, AutocompleteComponent],
  template: `<app-autocomplete inputId="place" [loader]="loader" placeholder="Pick one" [formControl]="control" />`,
})
class HostComponent {
  loader: OptionLoader = () => of({ items: [], page: 1, pageSize: PAGE_SIZE, totalCount: 0 });
  control = new FormControl('', Validators.required);
}

describe('AutocompleteComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let calls: { search: string; page: number }[];

  const element = () => fixture.nativeElement as HTMLElement;
  const input = () => element().querySelector<HTMLInputElement>('#place')!;
  const list = () => element().querySelector<HTMLElement>('.options');
  const options = () => Array.from(element().querySelectorAll('.option')).map((item) => item.textContent?.trim());
  const status = () => element().querySelector('.status')?.textContent?.trim();

  const press = (key: string) => {
    input().dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
    fixture.detectChanges();
  };

  function focus(): void {
    input().dispatchEvent(new Event('focus'));
    fixture.detectChanges();
  }

  function type(value: string): void {
    input().value = value;
    input().dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function scrollToEnd(): void {
    const element = list()!;
    element.scrollTop = element.scrollHeight;
    element.dispatchEvent(new Event('scroll'));
    fixture.detectChanges();
  }

  function useLoader(loader: OptionLoader): void {
    host.loader = (search, page) => {
      calls.push({ search, page });
      return loader(search, page);
    };
    fixture.detectChanges();
  }

  beforeEach(() => {
    calls = [];
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    useLoader(placesLoader(PLACES, PAGE_SIZE));
    fixture.detectChanges();
  });

  it('stays closed until the field is focused, then shows the first page only', () => {
    expect(options()).toEqual([]);
    expect(calls).toEqual([]);

    focus();

    expect(options()).toEqual(['Amsterdam', 'Antwerp', 'Bergen', 'Berlin', 'Hamburg']);
    expect(calls).toEqual([{ search: '', page: 1 }]);
    expect(input().placeholder).toBe('Pick one');
  });

  it('asks for the next page when the list is scrolled to the end, and stops when there is no more', () => {
    focus();

    scrollToEnd();
    expect(calls.at(-1)).toEqual({ search: '', page: 2 });
    expect(options().length).toBe(10);

    scrollToEnd();
    expect(options().length).toBe(12);

    scrollToEnd();
    expect(calls.length).toBe(3);
  });

  it('does not load another page while the list is not near its end', () => {
    focus();
    const scrollable = list()!;
    scrollable.scrollTop = 0;
    Object.defineProperty(scrollable, 'scrollHeight', { value: 1000, configurable: true });
    scrollable.dispatchEvent(new Event('scroll'));

    expect(calls.length).toBe(1);
  });

  it('searches on the server after the user pauses typing, starting again from the first page', fakeAsync(() => {
    focus();
    scrollToEnd();

    type('b');
    type('be');
    expect(calls.length).toBe(2);
    tick(250);
    fixture.detectChanges();

    expect(calls.at(-1)).toEqual({ search: 'be', page: 1 });
    expect(calls.length).toBe(3);
    expect(options()).toEqual(['Bergen', 'Berlin']);
  }));

  it('says so when nothing matches, and keeps what the user typed', fakeAsync(() => {
    focus();

    type('Zürich');
    tick(250);
    fixture.detectChanges();

    expect(options()).toEqual([]);
    expect(status()).toBe('No places found. Try another name.');
    expect(host.control.value).toBe('Zürich');
    expect(host.control.errors).toEqual({ notInList: true });
  }));

  it('shows that it is loading while the answer is on its way', () => {
    useLoader(() => new Observable<PagedResult<string>>());

    focus();

    expect(status()).toBe('Loading…');
  });

  it('offers a retry when the places cannot be loaded', () => {
    useLoader(() => throwError(() => new Error('down')));
    focus();
    expect(status()).toBe("Couldn't load places. Click to try again.");

    useLoader(placesLoader(PLACES, PAGE_SIZE));
    element().querySelector<HTMLElement>('.status')!.click();
    fixture.detectChanges();

    expect(options().length).toBe(PAGE_SIZE);
    expect(status()).toBeUndefined();
  });

  it('fills in the chosen place when an option is clicked, and closes', () => {
    focus();

    element().querySelector<HTMLElement>('.option')!.click();
    fixture.detectChanges();

    expect(host.control.value).toBe('Amsterdam');
    expect(input().value).toBe('Amsterdam');
    expect(host.control.valid).toBeTrue();
    expect(options()).toEqual([]);
  });

  it('only accepts text that is in the list: a place chosen, or typed exactly as it is offered', () => {
    focus();

    type('Atlantis');
    expect(host.control.errors).toEqual({ notInList: true });

    type(' hamburg ');
    expect(host.control.valid).toBeTrue();

    type('Hambur');
    expect(host.control.errors).toEqual({ notInList: true });
  });

  it('lists the first page again when it is reopened after a place was found by searching', fakeAsync(() => {
    focus();
    type('be');
    tick(250);
    fixture.detectChanges();
    element().querySelector<HTMLElement>('.option')!.click();
    fixture.detectChanges();
    calls.length = 0;

    focus();

    expect(calls).toEqual([{ search: '', page: 1 }]);
    expect(options().length).toBe(PAGE_SIZE);
  }));

  it('can be driven with the keyboard, and loads more when the last place is reached', () => {
    focus();

    press('ArrowDown');
    press('ArrowDown');
    expect(element().querySelector('.option--active')?.textContent?.trim()).toBe('Antwerp');
    expect(input().getAttribute('aria-activedescendant')).toContain('-1');

    press('ArrowUp');
    press('Enter');
    expect(host.control.value).toBe('Amsterdam');
    expect(options()).toEqual([]);

    focus();
    for (let i = 0; i < PAGE_SIZE; i++) {
      press('ArrowDown');
    }
    expect(calls.at(-1)).toEqual({ search: '', page: 2 });
  });

  it('stops enter from submitting the form when it is choosing an option, and lets it through otherwise', () => {
    focus();
    press('ArrowDown');
    const choosing = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    input().dispatchEvent(choosing);
    fixture.detectChanges();

    expect(choosing.defaultPrevented).toBeTrue();
    expect(host.control.value).toBe('Amsterdam');

    const afterwards = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    input().dispatchEvent(afterwards);
    expect(afterwards.defaultPrevented).toBeFalse();
  });

  it('closes on escape and on a click elsewhere, and marks the field touched', () => {
    focus();
    press('Escape');
    expect(options()).toEqual([]);

    focus();
    input().dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(options()).toEqual([]);
    expect(host.control.touched).toBeTrue();
  });

  it('shows the value the form sets and treats it as chosen, and clears when the form resets', () => {
    host.control.setValue('Oslo');
    fixture.detectChanges();
    expect(input().value).toBe('Oslo');
    expect(host.control.valid).toBeTrue();

    host.control.reset('');
    fixture.detectChanges();
    expect(input().value).toBe('');
  });

  it('describes itself as a combobox for assistive technology', () => {
    focus();

    expect(input().getAttribute('role')).toBe('combobox');
    expect(input().getAttribute('aria-expanded')).toBe('true');
    expect(element().querySelector('[role=listbox]')).not.toBeNull();
    expect(element().querySelectorAll('[role=option]').length).toBe(PAGE_SIZE);
  });

  it('can be disabled', () => {
    host.control.disable();
    fixture.detectChanges();

    expect(input().disabled).toBeTrue();
  });
});
