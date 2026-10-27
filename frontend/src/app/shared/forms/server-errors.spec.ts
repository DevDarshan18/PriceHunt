import { FormControl, FormGroup } from '@angular/forms';
import { ApiError } from '../../core/errors/api-error';
import { firstErrorMessage } from './error-messages';
import { applyServerErrors } from './server-errors';

describe('applyServerErrors', () => {
  function createForm() {
    return new FormGroup({
      fromDate: new FormControl(''),
      toLocation: new FormControl(''),
    });
  }

  it('puts the first server message on the matching field and marks it touched', () => {
    const form = createForm();

    applyServerErrors(form, new ApiError(400, 'Invalid', { fromDate: ['Too early.', 'Also bad.'] }));

    expect(form.controls.fromDate.errors).toEqual({ server: 'Too early.' });
    expect(form.controls.fromDate.touched).toBeTrue();
    expect(firstErrorMessage(form.controls.fromDate.errors)).toBe('Too early.');
  });

  it('handles several fields at once and leaves the others alone', () => {
    const form = createForm();
    form.controls.toLocation.setValue('Oslo');

    applyServerErrors(form, new ApiError(400, 'Invalid', { fromDate: ['Bad date.'] }));

    expect(form.controls.toLocation.errors).toBeNull();
    expect(form.controls.toLocation.touched).toBeFalse();
  });

  it('ignores errors for fields the form does not have', () => {
    const form = createForm();

    expect(() => applyServerErrors(form, new ApiError(400, 'Invalid', { unknown: ['Nope.'] }))).not.toThrow();
    expect(form.valid).toBeTrue();
  });

  it('clears the server error as soon as the user changes the value', () => {
    const form = createForm();
    applyServerErrors(form, new ApiError(400, 'Invalid', { fromDate: ['Too early.'] }));

    form.controls.fromDate.setValue('2026-12-01');

    expect(form.controls.fromDate.errors).toBeNull();
  });
});
