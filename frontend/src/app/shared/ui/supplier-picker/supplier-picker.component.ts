import { Component, ElementRef, computed, forwardRef, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Supplier } from '../../models/supplier.model';

const MAX_NAMES_SHOWN = 2;

@Component({
  selector: 'app-supplier-picker',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SupplierPickerComponent),
      multi: true,
    },
  ],
  host: {
    '(document:click)': 'closeOnOutsideClick($event)',
    '(keydown.escape)': 'close()',
  },
  templateUrl: './supplier-picker.component.html',
  styleUrl: './supplier-picker.component.scss',
})
export class SupplierPickerComponent implements ControlValueAccessor {
  readonly suppliers = input.required<Supplier[]>();
  readonly emptyLabel = input('Select suppliers');

  protected readonly selected = signal<string[]>([]);
  protected readonly disabled = signal(false);
  protected readonly open = signal(false);

  protected readonly summary = computed(() => {
    const names = this.suppliers()
      .filter((supplier) => this.selected().includes(supplier.id))
      .map((supplier) => supplier.name);

    if (names.length === 0) {
      return this.emptyLabel();
    }

    if (names.length === this.suppliers().length) {
      return 'All suppliers';
    }

    return names.length <= MAX_NAMES_SHOWN ? names.join(', ') : `${names.length} suppliers selected`;
  });

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private onChange: (value: string[]) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string[] | null): void {
    this.selected.set(value ?? []);
  }

  registerOnChange(callback: (value: string[]) => void): void {
    this.onChange = callback;
  }

  registerOnTouched(callback: () => void): void {
    this.onTouched = callback;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
    if (isDisabled) {
      this.open.set(false);
    }
  }

  protected toggleOpen(): void {
    if (this.open()) {
      this.close();
    } else {
      this.open.set(true);
    }
  }

  protected close(): void {
    if (this.open()) {
      this.open.set(false);
      this.onTouched();
    }
  }

  protected closeOnOutsideClick(event: Event): void {
    if (!this.host.contains(event.target as Node)) {
      this.close();
    }
  }

  protected isSelected(supplierId: string): boolean {
    return this.selected().includes(supplierId);
  }

  protected toggle(supplierId: string): void {
    const next = this.isSelected(supplierId)
      ? this.selected().filter((id) => id !== supplierId)
      : [...this.selected(), supplierId];
    this.update(next);
  }

  protected selectAll(): void {
    this.update(this.suppliers().map((supplier) => supplier.id));
  }

  protected clear(): void {
    this.update([]);
  }

  private update(value: string[]): void {
    this.selected.set(value);
    this.onChange(value);
  }
}
