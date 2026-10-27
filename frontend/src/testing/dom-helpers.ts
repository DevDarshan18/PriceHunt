export function typeInto(root: HTMLElement, selector: string, value: string): void {
  const input = root.querySelector<HTMLInputElement>(selector)!;
  input.dispatchEvent(new Event('focus'));
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

export function buttonLabelled(root: HTMLElement, label: string): HTMLButtonElement {
  return Array.from(root.querySelectorAll('button')).find((button) => button.textContent?.trim() === label)!;
}
