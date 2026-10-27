import { ValidationErrors } from '@angular/forms';

type MessageFactory = (error: ValidationErrors[string], label: string) => string;

const ERROR_MESSAGES: Record<string, MessageFactory> = {
  required: (_, label) => `${label} is required.`,
  maxlength: (error) => `Use at most ${error.requiredLength} characters.`,
  pastDate: () => "From date can't be in the past.",
  dateOrder: () => "To date can't be before the from date.",
  rangeTooLong: (error) => `Date range can't be longer than ${error.max} days.`,
  sameLocation: () => "Destination can't be the same as the origin.",
  minSelected: () => 'Select at least one supplier.',
  notInList: () => 'Choose a place from the list.',
  server: (message: string) => message,
};

export function firstErrorMessage(errors: ValidationErrors | null, label = 'This field'): string | null {
  if (!errors) {
    return null;
  }

  const [key, value] = Object.entries(errors)[0];
  return ERROR_MESSAGES[key]?.(value, label) ?? 'This value is not valid.';
}
