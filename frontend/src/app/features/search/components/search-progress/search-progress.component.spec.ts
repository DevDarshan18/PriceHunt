import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FinalSearchStatus } from '../../models/search.models';
import { SearchPhase } from '../../state/search.store';
import { SearchProgressComponent } from './search-progress.component';

describe('SearchProgressComponent', () => {
  let fixture: ComponentFixture<SearchProgressComponent>;

  function render(phase: SearchPhase, responded: number, selected: number, finalStatus: FinalSearchStatus | null = null) {
    fixture = TestBed.createComponent(SearchProgressComponent);
    fixture.componentRef.setInput('phase', phase);
    fixture.componentRef.setInput('responded', responded);
    fixture.componentRef.setInput('selected', selected);
    fixture.componentRef.setInput('finalStatus', finalStatus);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent!;
  }

  it('shows how many suppliers have responded while searching', () => {
    const text = render('searching', 5, 7);

    expect(text).toContain('5 of 7 suppliers responded');
    expect(text).toContain('Searching');
  });

  it('shows a clear final state', () => {
    expect(render('finished', 7, 7, 'Completed')).toContain('Completed');
    expect(render('finished', 6, 7, 'TimedOut')).toContain('Timed out');
    expect(render('finished', 2, 7, 'Cancelled')).toContain('Cancelled');
  });

  it('shows a failed search as failed', () => {
    expect(render('failed', 0, 0)).toContain('Failed');
  });
});
