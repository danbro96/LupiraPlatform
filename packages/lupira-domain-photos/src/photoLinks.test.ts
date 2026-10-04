import { describe, expect, it } from 'vitest';
import { linkedMessage, linkPhotosTitle, seeAllLinked } from './photoLinks.ts';

describe('photo link wording', () => {
  it('speaks of one photo or many', () => {
    expect(linkPhotosTitle(1)).toBe('Link to an event');
    expect(linkPhotosTitle(3)).toBe('Link 3 photos to an event');
    expect(linkedMessage(1, 1)).toBe('Linked to the event');
    expect(linkedMessage(3, 2)).toBe('Linked 2 photos');
    expect(seeAllLinked(5)).toBe('See all 5 in Photos');
  });
});
