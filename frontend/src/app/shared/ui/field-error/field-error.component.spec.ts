import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, Validators } from '@angular/forms';
import { FieldErrorComponent } from './field-error.component';

describe('FieldErrorComponent', () => {
  let fixture: ComponentFixture<FieldErrorComponent>;
  let control: FormControl<string | null>;

  const text = () => (fixture.nativeElement as HTMLElement).textContent?.trim();

  beforeEach(() => {
    control = new FormControl('', Validators.required);
    fixture = TestBed.createComponent(FieldErrorComponent);
    fixture.componentRef.setInput('control', control);
    fixture.detectChanges();
  });

  it('stays hidden until the field has been touched, even though it is invalid', () => {
    expect(control.invalid).toBeTrue();
    expect(text()).toBe('');
  });

  it('shows the first error once the field is touched', () => {
    control.markAsTouched();
    fixture.detectChanges();

    expect(text()).toBe('This field is required.');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role=alert]')).not.toBeNull();
  });

  it('also shows once the user has changed the value', () => {
    control.setValue('x');
    control.setValue('');
    control.markAsDirty();
    fixture.detectChanges();

    expect(text()).toBe('This field is required.');
  });

  it('disappears when the value becomes valid', () => {
    control.markAsTouched();
    control.setValue('Oslo');
    fixture.detectChanges();

    expect(text()).toBe('');
  });

  it('shows a message that came from the server', () => {
    control.setValue('Oslo');
    control.setErrors({ server: 'The API says no.' });
    control.markAsTouched();
    fixture.detectChanges();

    expect(text()).toBe('The API says no.');
  });
});
