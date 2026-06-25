/** JavaScript weekday: 0 = Sunday … 6 = Saturday. */
export function resolvePickupDatesForWeekdays(
  pickUpDate: Date,
  weekdays: number[],
): Date[] {
  const selected = new Set(
    weekdays.map((d) => Number(d)).filter((d) => Number.isInteger(d) && d >= 0 && d <= 6),
  );
  if (selected.size === 0) return [new Date(pickUpDate)];

  const start = new Date(pickUpDate);
  const dates: Date[] = [];
  for (let offset = 0; offset < 7; offset++) {
    const day = new Date(start);
    day.setDate(start.getDate() + offset);
    if (!selected.has(day.getDay())) continue;
    const withTime = new Date(day);
    withTime.setHours(
      start.getHours(),
      start.getMinutes(),
      start.getSeconds(),
      start.getMilliseconds(),
    );
    dates.push(withTime);
  }
  return dates;
}

export function shiftDateByPickupOffset(
  value: Date | null | undefined,
  sourcePickUp: Date,
  targetPickUp: Date,
): Date | null {
  if (!value) return null;
  const offsetMs = value.getTime() - sourcePickUp.getTime();
  return new Date(targetPickUp.getTime() + offsetMs);
}
