import { Supplier } from '../../../shared/models/supplier.model';
import { FinalSearchStatus, ResultRow, ResultRowStatus, SupplierResultEvent } from '../models/search.models';

export function buildRows(
  suppliers: Supplier[],
  results: SupplierResultEvent[],
  searching: boolean,
  finalStatus: FinalSearchStatus | null,
  waitedMilliseconds: number | null = null,
): ResultRow[] {
  const answered = results.map(toAnsweredRow).sort(compareAnswered);
  const answeredIds = new Set(results.map((result) => result.supplierId));
  const pendingStatus: ResultRowStatus = searching ? 'Waiting' : finalStatus === 'Cancelled' ? 'Cancelled' : 'TimedOut';

  const pending = suppliers
    .filter((supplier) => !answeredIds.has(supplier.id))
    .map<ResultRow>((supplier) => ({
      supplierId: supplier.id,
      supplierName: supplier.name,
      status: pendingStatus,
      price: null,
      currency: null,
      responseTimeMilliseconds: searching ? null : waitedMilliseconds,
    }))
    .sort((left, right) => left.supplierName.localeCompare(right.supplierName));

  return [...answered, ...pending];
}

function toAnsweredRow(result: SupplierResultEvent): ResultRow {
  return {
    supplierId: result.supplierId,
    supplierName: result.supplierName,
    status: result.status,
    price: result.price,
    currency: result.currency,
    responseTimeMilliseconds: result.responseTimeMilliseconds,
  };
}

function compareAnswered(left: ResultRow, right: ResultRow): number {
  if (left.price !== null && right.price !== null) {
    return left.price - right.price || left.supplierName.localeCompare(right.supplierName);
  }

  if (left.price !== null || right.price !== null) {
    return left.price !== null ? -1 : 1;
  }

  return left.supplierName.localeCompare(right.supplierName);
}
