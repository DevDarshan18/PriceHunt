import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HistoryItem, HistorySortField } from '../../models/history.models';
import { HistoryTableComponent } from './history-table.component';

const PRICED: HistoryItem = {
  searchId: 's1',
  searchedAtUtc: '2026-05-01T10:00:00Z',
  origin: 'Rotterdam',
  destination: 'Hamburg',
  supplierId: 'a',
  supplierName: 'Atlas Freight',
  price: 120.5,
  currency: 'USD',
  responseTimeMilliseconds: 900,
  status: 'Succeeded',
  errorCode: null,
};

const TIMED_OUT: HistoryItem = { ...PRICED, supplierId: 'g', supplierName: 'GlobalPorts', price: null, currency: null, status: 'TimedOut' };

describe('HistoryTableComponent', () => {
  let fixture: ComponentFixture<HistoryTableComponent>;

  const element = () => fixture.nativeElement as HTMLElement;
  const cells = (row: number) =>
    Array.from(element().querySelectorAll('tbody tr')[row].querySelectorAll('td')).map((cell) => cell.textContent?.replace(/\s+/g, ' ').trim());

  function render(items: HistoryItem[], loading = false) {
    fixture = TestBed.createComponent(HistoryTableComponent);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('sortBy', 'date');
    fixture.componentRef.setInput('descending', true);
    fixture.componentRef.setInput('loading', loading);
    fixture.detectChanges();
  }

  it('has a column for each thing the brief lists, plus the status', () => {
    render([]);

    const headers = Array.from(element().querySelectorAll('th')).map((header) => header.textContent?.trim());
    expect(headers.map((header) => header?.replace(/[▲▼]/g, '').trim())).toEqual(['Date', 'Route', 'Supplier', 'Status', 'Price', 'Response time']);
  });

  it('shows a row with route, supplier, status, price and response time', () => {
    render([PRICED]);

    const row = cells(0);
    expect(row[1]).toBe('Rotterdam → Hamburg');
    expect(row[2]).toBe('Atlas Freight');
    expect(row[3]).toBe('Succeeded');
    expect(row[4]).toBe('$120.50');
    expect(row[5]).toBe('0.90 s');
  });

  it('keeps the outcome in the status column and shows a dash when there is no price', () => {
    render([TIMED_OUT]);

    expect(cells(0)[3]).toBe('Timed out');
    expect(cells(0)[4]).toBe('—');
  });

  it('explains an empty table, and says when it is still loading', () => {
    render([]);
    expect(element().textContent).toContain('No results match these filters.');

    render([], true);
    expect(element().textContent).toContain('Loading');
  });

  it('marks the sorted column for assistive technology and emits the clicked column', () => {
    render([PRICED]);
    const clicked: HistorySortField[] = [];
    fixture.componentInstance.sortChanged.subscribe((field) => clicked.push(field));

    const headers = Array.from(element().querySelectorAll('th'));
    expect(headers.map((header) => header.getAttribute('aria-sort'))).toEqual(['descending', 'none', 'none', 'none', 'none', 'none']);

    headers[3].querySelector('button')!.click();
    headers[4].querySelector('button')!.click();
    headers[5].querySelector('button')!.click();

    expect(clicked).toEqual(['status', 'price', 'responseTime']);
  });
});
