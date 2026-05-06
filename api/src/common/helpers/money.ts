export function calcProfit(billed?: number | null, paid?: number | null): number | null {
  if (billed == null || paid == null) return null;
  return Number((billed - paid).toFixed(2));
}

export function calcMarginPercent(billed?: number | null, paid?: number | null): number | null {
  if (!billed || billed <= 0 || paid == null) return null;
  return Number((((billed - paid) / billed) * 100).toFixed(2));
}
