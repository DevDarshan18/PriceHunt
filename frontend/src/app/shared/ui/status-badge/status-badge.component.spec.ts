import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  let fixture: ComponentFixture<StatusBadgeComponent>;

  function render(status: string): HTMLElement {
    fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).querySelector('.badge')!;
  }

  const cases: [string, string, string][] = [
    ['Completed', 'Completed', 'success'],
    ['Succeeded', 'Succeeded', 'success'],
    ['TimedOut', 'Timed out', 'warning'],
    ['Cancelled', 'Cancelled', 'warning'],
    ['Failed', 'Failed', 'danger'],
    ['Running', 'Searching', 'neutral'],
    ['Waiting', 'Waiting', 'neutral'],
  ];

  for (const [status, label, tone] of cases) {
    it(`shows ${status} as "${label}" in the ${tone} style`, () => {
      const badge = render(status);

      expect(badge.textContent?.trim()).toBe(label);
      expect(badge.classList).toContain(`badge--${tone}`);
    });
  }

  it('shows an unknown status as it is, in the neutral style', () => {
    const badge = render('SomethingNew');

    expect(badge.textContent?.trim()).toBe('SomethingNew');
    expect(badge.classList).toContain('badge--neutral');
  });
});
