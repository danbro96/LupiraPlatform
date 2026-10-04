import { describe, expect, it } from 'vitest';
import { darkColors, lightColors, withAlpha } from './color.ts';
import { textOn } from './contrast.ts';

describe('textOn', () => {
  it('keeps white on saturated fills', () => {
    for (const color of [lightColors.primary, lightColors.danger, '#3a86c8', '#8a4fc4']) expect(textOn(color)).toBe('#ffffff');
  });

  it('switches to dark text on light fills, in every hex form', () => {
    for (const color of ['#fde047', '#FFF', '#fff8', '#fde047cc', darkColors.primary]) expect(textOn(color)).toBe(lightColors.text);
  });

  it('falls back to white for colours it cannot parse', () => {
    expect(textOn('rebeccapurple')).toBe('#ffffff');
  });
});

describe('withAlpha', () => {
  it('appends the alpha as two hex digits', () => {
    expect(withAlpha('#0d9488', 0.14)).toBe('#0d948824');
    expect(withAlpha('#0d9488', 1)).toBe('#0d9488ff');
  });

  it('clamps the alpha and replaces an existing one', () => {
    expect(withAlpha('#0d9488', 1.5)).toBe('#0d9488ff');
    expect(withAlpha('#0d9488', -1)).toBe('#0d948800');
    expect(withAlpha('#0d948824', 0.5)).toBe('#0d948880');
  });
});
