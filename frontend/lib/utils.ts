import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function fmtMoney(n?: number | null) {
  if (n == null) return '—';
  return n.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
}

export function fmtPercent(n?: number | null) {
  if (n == null) return '—';
  return `${n.toFixed(1)}%`;
}

export function fmtDate(d?: string | Date | null) {
  if (!d) return '—';
  return new Date(d).toLocaleDateString();
}
