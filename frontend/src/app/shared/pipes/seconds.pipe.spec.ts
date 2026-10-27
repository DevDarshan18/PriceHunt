import { SecondsPipe } from './seconds.pipe';

describe('SecondsPipe', () => {
  const pipe = new SecondsPipe();

  it('turns milliseconds into seconds with two decimals', () => {
    expect(pipe.transform(962)).toBe('0.96 s');
    expect(pipe.transform(4988)).toBe('4.99 s');
    expect(pipe.transform(6017)).toBe('6.02 s');
  });

  it('shows whole and zero values cleanly', () => {
    expect(pipe.transform(5000)).toBe('5.00 s');
    expect(pipe.transform(0)).toBe('0.00 s');
  });
});
