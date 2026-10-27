import { Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { firstErrorMessage } from '../../forms/error-messages';

@Component({
  selector: 'app-field-error',
  standalone: true,
  templateUrl: './field-error.component.html',
  styleUrl: './field-error.component.scss',
})
export class FieldErrorComponent {
  readonly control = input.required<AbstractControl>();
  readonly label = input('This field');

  protected get message(): string | null {
    const control = this.control();
    return control.touched || control.dirty ? firstErrorMessage(control.errors, this.label()) : null;
  }
}
