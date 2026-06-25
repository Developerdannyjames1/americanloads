/** 0 = Sunday … 6 = Saturday (JavaScript Date.getDay()). */
export const WEEKDAY_OPTIONS = [
  { value: 1, label: 'Mon' },
  { value: 2, label: 'Tue' },
  { value: 3, label: 'Wed' },
  { value: 4, label: 'Thu' },
  { value: 5, label: 'Fri' },
  { value: 6, label: 'Sat' },
  { value: 0, label: 'Sun' },
] as const;

export function countLoadsForWeekdays(pickUpDate: string | undefined, weekdays: number[] | undefined): number {
  if (!pickUpDate || !weekdays?.length) return 1;
  const start = new Date(pickUpDate);
  if (Number.isNaN(start.getTime())) return 1;
  const selected = new Set(weekdays.filter((d) => d >= 0 && d <= 6));
  if (!selected.size) return 1;
  let count = 0;
  for (let i = 0; i < 7; i++) {
    const d = new Date(start);
    d.setDate(start.getDate() + i);
    if (selected.has(d.getDay())) count++;
  }
  return count || 1;
}
