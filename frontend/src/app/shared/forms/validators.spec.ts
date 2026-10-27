import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { addDays, today } from '../utils/date.utils';
import { differentFrom, minSelected, notBefore, notInPast, revalidateWhenChanged, withinDaysOf } from './validators';

describe('form validators', () => {
  describe('notInPast', () => {
    const validate = (value: string) => notInPast()(new FormControl(value));

    it('accepts today and later dates', () => {
      expect(validate(today())).toBeNull();
      expect(validate(addDays(today(), 1))).toBeNull();
    });

    it('rejects yesterday and earlier dates', () => {
      expect(validate(addDays(today(), -1))).toEqual({ pastDate: true });
      expect(validate(addDays(today(), -2))).toEqual({ pastDate: true });
    });

    it('ignores an empty value so that required can report it', () => {
      expect(validate('')).toBeNull();
    });
  });

  describe('cross-field rules', () => {
    function createForm() {
      return new FormGroup({
        from: new FormControl('2026-05-10'),
        to: new FormControl('2026-05-12', [notBefore('from'), withinDaysOf('from', 90)]),
        origin: new FormControl('Oslo'),
        destination: new FormControl('Bergen', [differentFrom('origin')]),
      });
    }

    it('notBefore rejects an end date before the start date', () => {
      const form = createForm();
      form.controls.to.setValue('2026-05-09');

      expect(form.controls.to.errors).toEqual({ dateOrder: true });
    });

    it('withinDaysOf allows exactly the maximum and rejects one day more', () => {
      const form = createForm();

      form.controls.to.setValue(addDays('2026-05-10', 90));
      expect(form.controls.to.errors).toBeNull();

      form.controls.to.setValue(addDays('2026-05-10', 91));
      expect(form.controls.to.errors).toEqual({ rangeTooLong: { max: 90 } });
    });

    it('differentFrom rejects the same place ignoring case and spaces', () => {
      const form = createForm();
      form.controls.destination.setValue('  oslo ');

      expect(form.controls.destination.errors).toEqual({ sameLocation: true });
    });
  });

  it('minSelected requires the given number of items', () => {
    expect(minSelected()(new FormControl([]))).toEqual({ minSelected: { minimum: 1 } });
    expect(minSelected()(new FormControl(['a']))).toBeNull();
    expect(minSelected(2)(new FormControl(['a']))).toEqual({ minSelected: { minimum: 2 } });
  });

  it('revalidateWhenChanged checks the target again when the source changes', () => {
    @Component({ standalone: true, template: '' })
    class HostComponent {
      form = new FormGroup({
        from: new FormControl('2026-05-10'),
        to: new FormControl('2026-05-12', [notBefore('from')]),
      });

      constructor() {
        revalidateWhenChanged(this.form.controls.from, this.form.controls.to);
      }
    }

    const host = TestBed.createComponent(HostComponent).componentInstance;
    expect(host.form.controls.to.valid).toBeTrue();

    host.form.controls.from.setValue('2026-05-20');

    expect(host.form.controls.to.errors).toEqual({ dateOrder: true });
  });
});
