import { addDays, daysBetween, toIsoDate } from './date.utils';
import { toHttpParams } from './http-params';

describe('date utils', () => {
  it('formats a local date as yyyy-mm-dd', () => {
    expect(toIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('adds days across month ends', () => {
    expect(addDays('2026-01-30', 3)).toBe('2026-02-02');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('counts the days between two dates', () => {
    expect(daysBetween('2026-05-10', '2026-05-12')).toBe(2);
    expect(daysBetween('2026-05-12', '2026-05-10')).toBe(-2);
  });
});

describe('toHttpParams', () => {
  it('repeats a key for each list item', () => {
    const params = toHttpParams({ supplierIds: ['a', 'b'] });

    expect(params.getAll('supplierIds')).toEqual(['a', 'b']);
  });

  it('skips empty values but keeps false and zero', () => {
    const params = toHttpParams({ origin: '', to: null, from: undefined, ids: [], descending: false, page: 0 });

    expect(params.keys().sort()).toEqual(['descending', 'page']);
    expect(params.get('descending')).toBe('false');
    expect(params.get('page')).toBe('0');
  });
});
