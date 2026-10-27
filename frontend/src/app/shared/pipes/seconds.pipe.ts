import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'seconds', standalone: true })
export class SecondsPipe implements PipeTransform {
  transform(milliseconds: number): string {
    return `${(milliseconds / 1000).toFixed(2)} s`;
  }
}
