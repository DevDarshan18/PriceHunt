import { of } from 'rxjs';
import { SupplierResultEvent } from '../app/features/search/models/search.models';
import { PagedResult } from '../app/shared/models/paged-result.model';
import { Supplier } from '../app/shared/models/supplier.model';

export const SUPPLIERS: Supplier[] = [
  { id: 'a', name: 'Atlas' },
  { id: 'b', name: 'BlueWave' },
  { id: 'c', name: 'Cargo' },
  { id: 'd', name: 'Delta' },
];

export const LOCATIONS = ['Bergen', 'Hamburg', 'Oslo', 'Rotterdam'];

export function placesLoader(places: string[], pageSize = 10) {
  return (search: string, page: number) => {
    const text = search.toLowerCase();
    const matches = places
      .filter((place) => place.toLowerCase().includes(text))
      .sort((a, b) => Number(!b.toLowerCase().startsWith(text)) - Number(!a.toLowerCase().startsWith(text)) || a.localeCompare(b));
    const result: PagedResult<string> = {
      items: matches.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalCount: matches.length,
    };
    return of(result);
  };
}

export function supplierResult(supplierId: string, price: number | null, searchId = 's1'): SupplierResultEvent {
  const supplier = SUPPLIERS.find((item) => item.id === supplierId)!;

  return {
    searchId,
    supplierId,
    supplierName: supplier.name,
    status: price === null ? 'Failed' : 'Succeeded',
    price,
    currency: price === null ? null : 'USD',
    responseTimeMilliseconds: 100,
    errorCode: price === null ? 'SUPPLIER_FAILURE' : null,
  };
}
