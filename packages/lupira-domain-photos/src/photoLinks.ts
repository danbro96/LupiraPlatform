/** The cal-api relation that ties a photo to the event it depicts. */
export const PHOTO_LINK = { toKind: 'photo', relationType: 'depicts' } as const;

/** Photo wording both apps show. */
export const PHOTO_TEXT = {
  movedToTrash: 'Moved to trash',
  trashEmptied: 'Trash emptied',
  emptyTrashFailed: 'Could not empty the trash',
  deletedForGood: 'Deleted for good',
  deleteFailed: 'Could not delete the photo.',
  emptyTrashWarning: 'Every photo in the trash is deleted for good. It cannot be undone.',
  noLocation: 'No location — this photo never appears on the map.',
  noneLinked: 'No photos linked.',
  noneAround: 'No photos from this time.',
  takenDuring: 'Taken during this event:',
  noEventsAround: 'No events around this time.',
} as const;

export function linkPhotosTitle(count: number): string {
  return count === 1 ? 'Link to an event' : `Link ${count} photos to an event`;
}

export function linkedMessage(count: number, linked: number): string {
  return count === 1 ? 'Linked to the event' : `Linked ${linked} photos`;
}

export function seeAllLinked(n: number): string {
  return `See all ${n} in Photos`;
}
