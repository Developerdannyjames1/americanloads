export const ROLES = {
  Admin: 'admin',
  Shipper: 'shipper',
  Carrier: 'carrier',
  Dispatcher: 'dispatcher',
} as const;

export const LOAD_STATUS = [
  'draft',
  'posted',
  'claimed',
  'assigned',
  'in_transit',
  'delivered',
  'completed',
  'cancelled',
] as const;

export const STATUS_COLORS: Record<string, string> = {
  draft: 'bg-slate-100 text-slate-700',
  posted: 'bg-blue-100 text-blue-700',
  claimed: 'bg-amber-100 text-amber-700',
  assigned: 'bg-indigo-100 text-indigo-700',
  in_transit: 'bg-violet-100 text-violet-700',
  delivered: 'bg-emerald-100 text-emerald-700',
  completed: 'bg-green-100 text-green-700',
  cancelled: 'bg-rose-100 text-rose-700',
};
