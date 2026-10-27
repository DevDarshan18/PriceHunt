import { SUPPLIERS, supplierResult as result } from '../../../../testing/search-fixtures';
import { buildRows } from './search-rows';

describe('buildRows', () => {
  it('lists the cheapest price first, then failures, then suppliers still waiting', () => {
    const rows = buildRows(SUPPLIERS, [result('c', 300), result('a', null), result('b', 100)], true, null);

    expect(rows.map((row) => [row.supplierName, row.status])).toEqual([
      ['BlueWave', 'Succeeded'],
      ['Cargo', 'Succeeded'],
      ['Atlas', 'Failed'],
      ['Delta', 'Waiting'],
    ]);
  });

  it('puts equal prices in name order so the list does not shuffle', () => {
    const rows = buildRows(SUPPLIERS, [result('b', 50), result('a', 50)], true, null);

    expect(rows.slice(0, 2).map((row) => row.supplierName)).toEqual(['Atlas', 'BlueWave']);
  });

  it('marks suppliers that never answered once the search has ended', () => {
    const timedOut = buildRows(SUPPLIERS, [result('a', 10)], false, 'TimedOut');
    const cancelled = buildRows(SUPPLIERS, [result('a', 10)], false, 'Cancelled');

    expect(timedOut.slice(1).every((row) => row.status === 'TimedOut')).toBeTrue();
    expect(cancelled.slice(1).every((row) => row.status === 'Cancelled')).toBeTrue();
  });

  it('shows how long the search waited for suppliers that never answered, but not while it is running', () => {
    const running = buildRows(SUPPLIERS, [result('a', 10)], true, null, 6000);
    const ended = buildRows(SUPPLIERS, [result('a', 10)], false, 'TimedOut', 6000);

    expect(running.slice(1).every((row) => row.responseTimeMilliseconds === null)).toBeTrue();
    expect(ended.slice(1).every((row) => row.responseTimeMilliseconds === 6000)).toBeTrue();
  });

  it('shows every supplier as waiting before anything has arrived', () => {
    const rows = buildRows(SUPPLIERS, [], true, null);

    expect(rows.length).toBe(4);
    expect(rows.every((row) => row.status === 'Waiting')).toBeTrue();
  });
});
