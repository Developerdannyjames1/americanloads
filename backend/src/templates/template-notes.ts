/** Pack description + user notes into LoadTemplates.Notes (single DB column). */
export function packTemplateNotes(
  description: string | null,
  userNotes: string | null,
): string | null {
  const d = (description || '').trim();
  const u = (userNotes || '').trim();
  if (!d && !u) return null;
  // User notes present → JSON so both fields round-trip (fits Notes nvarchar(1000)).
  if (u) return JSON.stringify({ d, u });
  return d;
}

/** Unpack Notes into separate description / userNotes (legacy plain text → description only). */
export function unpackTemplateNotes(notes: string | null | undefined): {
  description: string;
  userNotes: string;
} {
  if (!notes) return { description: '', userNotes: '' };
  const raw = notes.trim();
  if (!raw) return { description: '', userNotes: '' };
  if (raw.startsWith('{')) {
    try {
      const parsed = JSON.parse(raw) as { d?: unknown; u?: unknown };
      if (parsed && typeof parsed === 'object' && ('d' in parsed || 'u' in parsed)) {
        return {
          description: String(parsed.d ?? '').trim(),
          userNotes: String(parsed.u ?? '').trim(),
        };
      }
    } catch {
      /* fall through — treat as legacy plain text */
    }
  }
  return { description: raw, userNotes: '' };
}
