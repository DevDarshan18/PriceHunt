import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApiError } from '../../../core/errors/api-error';
import { ErrorMessageComponent } from './error-message.component';

describe('ErrorMessageComponent', () => {
  let fixture: ComponentFixture<ErrorMessageComponent>;

  function render(error: ApiError | null): string {
    fixture = TestBed.createComponent(ErrorMessageComponent);
    fixture.componentRef.setInput('error', error);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent?.trim() ?? '';
  }

  it('shows the message of an unexpected error as an alert', () => {
    expect(render(new ApiError(500, 'The server is down.'))).toBe('The server is down.');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role=alert]')).not.toBeNull();
  });

  it('shows nothing when there is no error', () => {
    expect(render(null)).toBe('');
  });

  it('shows nothing for a validation error because the form shows it next to the field', () => {
    expect(render(new ApiError(400, 'Invalid', { fromDate: ['Too early.'] }))).toBe('');
  });
});
