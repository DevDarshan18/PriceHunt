import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { daysBetween, today } from '../utils/date.utils';

function siblingValue(control: AbstractControl, siblingName: string): unknown {
  return control.parent?.get(siblingName)?.value;
}

export function notInPast(): ValidatorFn {
  return (control) => {
    const value = control.value as string;
    return value && value < today() ? { pastDate: true } : null;
  };
}

export function notBefore(otherControlName: string): ValidatorFn {
  return (control) => {
    const other = siblingValue(control, otherControlName) as string;
    return control.value && other && control.value < other ? { dateOrder: true } : null;
  };
}

export function withinDaysOf(otherControlName: string, maxDays: number): ValidatorFn {
  return (control) => {
    const other = siblingValue(control, otherControlName) as string;
    return control.value && other && daysBetween(other, control.value) > maxDays ? { rangeTooLong: { max: maxDays } } : null;
  };
}

export function differentFrom(otherControlName: string): ValidatorFn {
  return (control) => {
    const value = String(control.value ?? '').trim().toLowerCase();
    const other = String(siblingValue(control, otherControlName) ?? '').trim().toLowerCase();
    return value && value === other ? { sameLocation: true } : null;
  };
}

export function minSelected(minimum = 1): ValidatorFn {
  return (control): ValidationErrors | null => {
    const selected = (control.value as unknown[] | null) ?? [];
    return selected.length < minimum ? { minSelected: { minimum } } : null;
  };
}

export function revalidateWhenChanged(source: AbstractControl, target: AbstractControl): void {
  source.valueChanges
    .pipe(takeUntilDestroyed())
    .subscribe(() => target.updateValueAndValidity({ emitEvent: false }));
}
