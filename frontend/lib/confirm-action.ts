/** Generic OK/Cancel confirm (e.g. accept / reject flows). */
export function confirmPrompt(message: string): boolean {
  if (typeof window === 'undefined') return false;
  return window.confirm(message);
}

/** Browser confirm for destructive actions — consistent copy across the app. */

export function confirmDelete(opts: { subject: string; name?: string }): boolean {
  if (typeof window === 'undefined') return false;
  const detail = opts.name ? `\n\n${opts.name}` : '';
  return window.confirm(`Delete ${opts.subject}?${detail}\n\nThis cannot be undone.`);
}

/** Confirm duplicating an entity (loads: creates a draft copy). */
export function confirmDuplicate(opts: { detail?: string } = {}): boolean {
  if (typeof window === 'undefined') return false;
  const extra = opts.detail ? `\n\n${opts.detail}` : '';
  return window.confirm(
    `Duplicate this load?${extra}\n\nA new draft copy will be created.`,
  );
}
