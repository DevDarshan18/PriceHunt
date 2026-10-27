import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { Supplier } from '../models/supplier.model';

@Injectable({ providedIn: 'root' })
export class SuppliersService {
  private readonly http = inject(HttpClient);

  readonly suppliers = toSignal(
    this.http.get<Supplier[]>('/api/suppliers').pipe(catchError(() => of([] as Supplier[]))),
    { initialValue: [] as Supplier[] },
  );
}
