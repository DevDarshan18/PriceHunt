import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PagerComponent } from './pager.component';

describe('PagerComponent', () => {
  let fixture: ComponentFixture<PagerComponent>;

  const buttons = () => Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button'));

  function render(page: number, pageSize: number, totalCount: number) {
    fixture = TestBed.createComponent(PagerComponent);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', pageSize);
    fixture.componentRef.setInput('totalCount', totalCount);
    fixture.detectChanges();
  }

  it('shows the page position and the number of results', () => {
    render(2, 20, 45);

    expect(fixture.nativeElement.textContent).toContain('Page 2 of 3');
    expect(fixture.nativeElement.textContent).toContain('45 results');
  });

  it('has one page when there are no results', () => {
    render(1, 20, 0);

    expect(fixture.nativeElement.textContent).toContain('Page 1 of 1');
  });

  it('disables previous on the first page and next on the last page', () => {
    render(1, 20, 45);
    expect(buttons().map((button) => button.disabled)).toEqual([true, false]);

    fixture.componentRef.setInput('page', 3);
    fixture.detectChanges();
    expect(buttons().map((button) => button.disabled)).toEqual([false, true]);
  });

  it('emits the new page and the new page size', () => {
    render(2, 20, 100);
    const pages: number[] = [];
    const sizes: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => pages.push(page));
    fixture.componentInstance.pageSizeChange.subscribe((size) => sizes.push(size));

    buttons()[1].click();
    buttons()[0].click();
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select');
    select.value = '50';
    select.dispatchEvent(new Event('change'));

    expect(pages).toEqual([3, 1]);
    expect(sizes).toEqual([50]);
  });
});
