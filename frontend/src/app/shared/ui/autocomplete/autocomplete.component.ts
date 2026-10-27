import { Component, ElementRef, computed, forwardRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, ControlValueAccessor, NG_VALIDATORS, NG_VALUE_ACCESSOR, ValidationErrors, Validator } from '@angular/forms';
import { EMPTY, Observable, Subject, catchError, debounceTime, map, switchMap, tap } from 'rxjs';
import { PagedResult } from '../../models/paged-result.model';

export type OptionLoader = (search: string, page: number) => Observable<PagedResult<string>>;

interface OptionRequest {
  search: string;
  page: number;
}

const SEARCH_DELAY_MS = 250;
const SCROLL_END_TOLERANCE_PX = 24;
let nextId = 0;

@Component({
  selector: 'app-autocomplete',
  standalone: true,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => AutocompleteComponent), multi: true },
    { provide: NG_VALIDATORS, useExisting: forwardRef(() => AutocompleteComponent), multi: true },
  ],
  templateUrl: './autocomplete.component.html',
  styleUrl: './autocomplete.component.scss',
})
export class AutocompleteComponent implements ControlValueAccessor, Validator {
  readonly loader = input.required<OptionLoader>();
  readonly inputId = input('');
  readonly placeholder = input('');

  protected readonly text = signal('');
  protected readonly open = signal(false);
  protected readonly activeIndex = signal(-1);
  protected readonly disabled = signal(false);
  protected readonly options = signal<string[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly failed = signal(false);
  protected readonly searched = signal(false);
  protected readonly listId = `autocomplete-${nextId++}`;

  protected readonly hasMore = computed(() => this.options().length < this.totalCount());
  protected readonly showPanel = computed(
    () => this.open() && (this.options().length > 0 || this.searched() || this.loading() || this.failed()),
  );
  protected readonly isEmpty = computed(() => this.searched() && !this.loading() && !this.failed() && this.options().length === 0);
  protected readonly activeOptionId = computed(() => (this.showPanel() && this.activeIndex() >= 0 ? `${this.listId}-${this.activeIndex()}` : null));

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly typed = new Subject<string>();
  private readonly requests = new Subject<OptionRequest>();

  private selected: string | null = null;
  private current: OptionRequest | null = null;
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};
  private onValidatorChange: () => void = () => {};

  constructor() {
    this.typed.pipe(debounceTime(SEARCH_DELAY_MS), takeUntilDestroyed()).subscribe((search) => this.load(search, 1));

    this.requests
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap((request) =>
          this.loader()(request.search, request.page).pipe(
            map((result) => ({ request, result })),
            catchError(() => {
              this.loading.set(false);
              this.failed.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ request, result }) => {
        this.options.update((loaded) => (request.page === 1 ? result.items : [...loaded, ...result.items]));
        this.totalCount.set(result.totalCount);
        this.searched.set(true);
        this.loading.set(false);
        this.onValidatorChange();
      });
  }

  writeValue(value: string | null): void {
    this.text.set(value ?? '');
    this.selected = value || null;
  }

  registerOnChange(callback: (value: string) => void): void {
    this.onChange = callback;
  }

  registerOnTouched(callback: () => void): void {
    this.onTouched = callback;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  validate(_: AbstractControl): ValidationErrors | null {
    const value = this.text().trim().toLowerCase();
    if (!value) {
      return null;
    }

    const known = value === this.selected?.toLowerCase() || this.options().some((option) => option.toLowerCase() === value);
    return known ? null : { notInList: true };
  }

  registerOnValidatorChange(callback: () => void): void {
    this.onValidatorChange = callback;
  }

  protected openList(): void {
    this.open.set(true);

    const search = this.searchTerm();
    if (this.current?.search !== search) {
      this.load(search, 1);
    }
  }

  protected onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.text.set(value);
    this.selected = null;
    this.onChange(value);
    this.activeIndex.set(-1);
    this.open.set(true);
    this.typed.next(this.searchTerm());
  }

  protected onKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.open.set(true);
        this.moveActive(1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.moveActive(-1);
        break;
      case 'Enter':
        if (this.showPanel() && this.activeIndex() >= 0) {
          event.preventDefault();
          this.select(this.options()[this.activeIndex()]);
        }
        break;
      case 'Escape':
        this.open.set(false);
        break;
    }
  }

  protected onScroll(event: Event): void {
    const list = event.target as HTMLElement;
    if (list.scrollTop + list.clientHeight >= list.scrollHeight - SCROLL_END_TOLERANCE_PX) {
      this.loadMore();
    }
  }

  protected select(option: string): void {
    this.text.set(option);
    this.selected = option;
    this.onChange(option);
    this.open.set(false);
  }

  protected retry(): void {
    if (this.current) {
      this.load(this.current.search, this.current.page);
    }
  }

  protected onBlur(): void {
    this.open.set(false);
    this.onTouched();
  }

  private searchTerm(): string {
    return this.text() === this.selected ? '' : this.text().trim();
  }

  private load(search: string, page: number): void {
    this.current = { search, page };
    this.requests.next(this.current);
  }

  private loadMore(): void {
    if (this.current && this.hasMore() && !this.loading() && !this.failed()) {
      this.load(this.current.search, this.current.page + 1);
    }
  }

  private moveActive(step: number): void {
    const last = this.options().length - 1;
    if (last < 0) {
      return;
    }

    const next = Math.min(Math.max(this.activeIndex() + step, 0), last);
    this.activeIndex.set(next);
    this.host.nativeElement.querySelector(`#${this.listId}-${next}`)?.scrollIntoView({ block: 'nearest' });

    if (next === last) {
      this.loadMore();
    }
  }
}
