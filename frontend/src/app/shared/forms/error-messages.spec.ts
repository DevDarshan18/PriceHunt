import { firstErrorMessage } from './error-messages';

describe('firstErrorMessage', () => {
  it('names the field in the required message', () => {
    expect(firstErrorMessage({ required: true }, 'Origin')).toBe('Origin is required.');
  });

  it('returns nothing when there are no errors', () => {
    expect(firstErrorMessage(null)).toBeNull();
  });

  it('has a message for each rule the forms use', () => {
    expect(firstErrorMessage({ required: true })).toBe('This field is required.');
    expect(firstErrorMessage({ maxlength: { requiredLength: 100, actualLength: 120 } })).toBe('Use at most 100 characters.');
    expect(firstErrorMessage({ pastDate: true })).toBe("From date can't be in the past.");
    expect(firstErrorMessage({ dateOrder: true })).toBe("To date can't be before the from date.");
    expect(firstErrorMessage({ rangeTooLong: { max: 90 } })).toBe("Date range can't be longer than 90 days.");
    expect(firstErrorMessage({ sameLocation: true })).toBe("Destination can't be the same as the origin.");
    expect(firstErrorMessage({ minSelected: { minimum: 1 } })).toBe('Select at least one supplier.');
    expect(firstErrorMessage({ notInList: true })).toBe('Choose a place from the list.');
  });

  it('shows a message from the server as it is', () => {
    expect(firstErrorMessage({ server: 'The API says no.' })).toBe('The API says no.');
  });

  it('only reports the first error, and falls back for an unknown rule', () => {
    expect(firstErrorMessage({ required: true, pastDate: true })).toBe('This field is required.');
    expect(firstErrorMessage({ somethingNew: true })).toBe('This value is not valid.');
  });
});
