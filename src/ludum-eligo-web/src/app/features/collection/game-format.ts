/** "2–4", "2", "2+", "up to 4", or null when both ends are unknown. */
export function formatRange(min: number | null, max: number | null): string | null {
  if (min !== null && max !== null) {
    return min === max ? `${min}` : `${min}–${max}`;
  }
  if (min !== null) {
    return `${min}+`;
  }
  return max !== null ? `up to ${max}` : null;
}

export function formatPlayers(min: number | null, max: number | null): string | null {
  const range = formatRange(min, max);
  return range === null ? null : `${range} ${range === '1' ? 'player' : 'players'}`;
}

export function formatPlaytime(min: number | null, max: number | null): string | null {
  const range = formatRange(min, max);
  return range === null ? null : `${range} min`;
}
