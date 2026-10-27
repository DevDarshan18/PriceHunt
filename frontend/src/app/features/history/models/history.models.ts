export type HistorySortField = 'date' | 'route' | 'supplier' | 'status' | 'price' | 'responseTime';

export interface HistoryFilters {
  from: string;
  to: string;
  supplierIds: string[];
  origin: string;
  destination: string;
}

export interface HistoryQuery extends HistoryFilters {
  sortBy: HistorySortField;
  descending: boolean;
  page: number;
  pageSize: number;
}

export interface HistoryItem {
  searchId: string;
  searchedAtUtc: string;
  origin: string;
  destination: string;
  supplierId: string;
  supplierName: string;
  price: number | null;
  currency: string | null;
  responseTimeMilliseconds: number;
  status: string;
  errorCode: string | null;
}
