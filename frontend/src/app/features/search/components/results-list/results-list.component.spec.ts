import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResultRow } from '../../models/search.models';
import { ResultsListComponent } from './results-list.component';

describe('ResultsListComponent', () => {
  let fixture: ComponentFixture<ResultsListComponent>;

  const rows: ResultRow[] = [
    { supplierId: 'a', supplierName: 'Atlas', status: 'Succeeded', price: 1204.7, currency: 'USD', responseTimeMilliseconds: 1751 },
    { supplierId: 'b', supplierName: 'BlueWave', status: 'Failed', price: null, currency: null, responseTimeMilliseconds: 800 },
    { supplierId: 'c', supplierName: 'Cargo', status: 'Waiting', price: null, currency: null, responseTimeMilliseconds: null },
  ];

  const items = () => Array.from(fixture.nativeElement.querySelectorAll('li') as NodeListOf<HTMLElement>);
  const describeRow = (item: HTMLElement) =>
    [
      item.querySelector('.result__name'),
      item.querySelector('.result__status'),
      item.querySelector('.result__price'),
      item.querySelector('.result__time'),
    ].map((part) => part?.textContent?.trim() ?? '');

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultsListComponent);
    fixture.componentRef.setInput('rows', rows);
    fixture.detectChanges();
  });

  it('labels the four columns', () => {
    const labels = Array.from(fixture.nativeElement.querySelectorAll('.columns span') as NodeListOf<HTMLElement>);

    expect(labels.map((label) => label.textContent?.trim())).toEqual(['Supplier', 'Status', 'Price', 'Response time']);
  });

  it('shows the status of every supplier, and the price when there is one', () => {
    expect(describeRow(items()[0])).toEqual(['Atlas', 'Succeeded', '$1,204.70', '1.75 s']);
    expect(describeRow(items()[1])).toEqual(['BlueWave', 'Failed', '—', '0.80 s']);
    expect(describeRow(items()[2])).toEqual(['Cargo', 'Waiting', '—', '']);
  });

  it('keeps the same element for a supplier when the order changes', () => {
    const atlas = items()[0];

    fixture.componentRef.setInput('rows', [rows[1], rows[2], rows[0]]);
    fixture.detectChanges();

    expect(items()[2]).toBe(atlas);
    expect(items().map((item) => item.style.getPropertyValue('--row-index'))).toEqual(['0', '1', '2']);
  });
});
