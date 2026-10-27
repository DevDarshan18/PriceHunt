import { Supplier } from '../../../shared/models/supplier.model';

export interface SearchRequest {
  fromLocation: string;
  toLocation: string;
  fromDate: string;
  toDate: string;
  supplierIds: string[];
}

export type FinalSearchStatus = 'Completed' | 'TimedOut' | 'Cancelled';

export interface SearchStartedEvent {
  searchId: string;
  createdAtUtc: string;
  suppliers: Supplier[];
}

export interface SupplierResultEvent {
  searchId: string;
  supplierId: string;
  supplierName: string;
  status: 'Succeeded' | 'Failed';
  price: number | null;
  currency: string | null;
  responseTimeMilliseconds: number;
  errorCode: string | null;
}

export interface SearchFinishedEvent {
  searchId: string;
  status: FinalSearchStatus;
  selectedCount: number;
  respondedCount: number;
}

export type SearchEvent =
  | { type: 'started'; data: SearchStartedEvent }
  | { type: 'supplier'; data: SupplierResultEvent }
  | { type: 'finished'; data: SearchFinishedEvent };

export type ResultRowStatus = 'Succeeded' | 'Failed' | 'Waiting' | 'TimedOut' | 'Cancelled';

export interface ResultRow {
  supplierId: string;
  supplierName: string;
  status: ResultRowStatus;
  price: number | null;
  currency: string | null;
  responseTimeMilliseconds: number | null;
}
