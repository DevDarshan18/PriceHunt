import { Signal, effect, untracked } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { ApiError } from '../../core/errors/api-error';

export function applyServerErrors(form: FormGroup, error: ApiError): void {
  for (const [field, messages] of Object.entries(error.errors)) {
    const control = form.get(field);
    control?.setErrors({ server: messages[0] });
    control?.markAsTouched();
  }
}

export function showServerErrors(form: FormGroup, serverError: Signal<ApiError | null>): void {
  effect(() => {
    const error = serverError();
    if (error?.isValidation) {
      untracked(() => applyServerErrors(form, error));
    }
  });
}
