import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { configureHttp } from '../../../testing/http-testing';
import { SUPPLIERS } from '../../../testing/search-fixtures';
import { SuppliersService } from './suppliers.service';

describe('SuppliersService', () => {
  let controller: HttpTestingController;

  beforeEach(() => {
    configureHttp();
    controller = TestBed.inject(HttpTestingController);
  });

  it('starts empty and fills in once the suppliers have loaded', () => {
    const service = TestBed.inject(SuppliersService);
    expect(service.suppliers()).toEqual([]);

    controller.expectOne('/api/suppliers').flush(SUPPLIERS);

    expect(service.suppliers()).toEqual(SUPPLIERS);
  });

  it('loads the suppliers only once, however many components ask', () => {
    TestBed.inject(SuppliersService);
    TestBed.inject(SuppliersService);

    expect(controller.match('/api/suppliers').length).toBe(1);
  });

  it('falls back to an empty list when the request fails', () => {
    const service = TestBed.inject(SuppliersService);

    controller.expectOne('/api/suppliers').flush('down', { status: 500, statusText: 'Server Error' });

    expect(service.suppliers()).toEqual([]);
  });
});
