import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PriceCellComponent } from './price-cell.component';

describe('PriceCellComponent', () => {
  let fixture: ComponentFixture<PriceCellComponent>;

  function render(price: number | null, currency: string | null): string | undefined {
    fixture = TestBed.createComponent(PriceCellComponent);
    fixture.componentRef.setInput('price', price);
    fixture.componentRef.setInput('currency', currency);
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).textContent?.trim();
  }

  it('shows a price in its currency', () => {
    expect(render(1204.7, 'USD')).toBe('$1,204.70');
  });

  it('defaults to dollars when the currency is missing', () => {
    expect(render(10, null)).toBe('$10.00');
  });

  it('shows a dash when there is no price', () => {
    expect(render(null, null)).toBe('—');
  });

  it('shows a free price of zero as a price, not as a missing one', () => {
    expect(render(0, 'USD')).toBe('$0.00');
  });
});
