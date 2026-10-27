import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { buttonLabelled } from '../../../../testing/dom-helpers';
import { Supplier } from '../../models/supplier.model';
import { SupplierPickerComponent } from './supplier-picker.component';

const SUPPLIERS: Supplier[] = [
  { id: 'a', name: 'Atlas' },
  { id: 'b', name: 'BlueWave' },
  { id: 'c', name: 'Cargo' },
];

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, SupplierPickerComponent],
  template: `
    <app-supplier-picker [suppliers]="suppliers" emptyLabel="Nothing yet" [formControl]="control" />
    <p id="outside">Somewhere else</p>
  `,
})
class HostComponent {
  suppliers = SUPPLIERS;
  control = new FormControl<string[]>(['a']);
}

describe('SupplierPickerComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  const element = () => fixture.nativeElement as HTMLElement;
  const trigger = () => element().querySelector<HTMLButtonElement>('.picker__trigger')!;
  const panel = () => element().querySelector('.picker__panel');
  const checkboxes = () => Array.from(element().querySelectorAll<HTMLInputElement>('input[type=checkbox]'));

  function open(): void {
    trigger().click();
    fixture.detectChanges();
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('summary', () => {
    it('names one or two suppliers', () => {
      expect(trigger().textContent).toContain('Atlas');

      host.control.setValue(['a', 'b']);
      fixture.detectChanges();
      expect(trigger().textContent).toContain('Atlas, BlueWave');
    });

    it('counts three or more, and says when all are selected', () => {
      host.control.setValue(['a', 'b', 'c']);
      fixture.detectChanges();

      expect(trigger().textContent).toContain('All suppliers');
    });

    it('shows the empty label when nothing is selected', () => {
      host.control.setValue([]);
      fixture.detectChanges();

      expect(trigger().textContent).toContain('Nothing yet');
    });
  });

  describe('dropdown', () => {
    it('is closed until the trigger is clicked', () => {
      expect(panel()).toBeNull();

      open();

      expect(panel()).not.toBeNull();
      expect(trigger().getAttribute('aria-expanded')).toBe('true');
    });

    it('shows the form value as checked boxes', () => {
      open();

      expect(checkboxes().map((box) => box.checked)).toEqual([true, false, false]);
    });

    it('writes a toggled supplier back to the form', () => {
      open();

      checkboxes()[1].click();
      expect(host.control.value).toEqual(['a', 'b']);

      checkboxes()[0].click();
      expect(host.control.value).toEqual(['b']);
    });

    it('selects every supplier and clears them again', () => {
      open();

      buttonLabelled(element(), 'Select all').click();
      expect(host.control.value).toEqual(['a', 'b', 'c']);

      buttonLabelled(element(), 'Clear').click();
      expect(host.control.value).toEqual([]);
    });

    it('closes on a click outside, and marks the control as touched', () => {
      open();

      element().querySelector<HTMLElement>('#outside')!.click();
      fixture.detectChanges();

      expect(panel()).toBeNull();
      expect(host.control.touched).toBeTrue();
    });

    it('closes on escape', () => {
      open();

      element().querySelector('app-supplier-picker')!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      fixture.detectChanges();

      expect(panel()).toBeNull();
    });

    it('stays open when a supplier is chosen so several can be picked', () => {
      open();

      checkboxes()[2].click();
      fixture.detectChanges();

      expect(panel()).not.toBeNull();
    });
  });

  it('cannot be opened when the control is disabled', () => {
    host.control.disable();
    fixture.detectChanges();

    expect(trigger().disabled).toBeTrue();
  });
});
