import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-price-cell',
  standalone: true,
  imports: [CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './price-cell.component.html',
})
export class PriceCellComponent {
  readonly price = input.required<number | null>();
  readonly currency = input<string | null>(null);
}
