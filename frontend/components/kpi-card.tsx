import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { cn } from '@/lib/utils';

const BAR = {
  sky: 'from-sky-400 to-sky-600',
  cyan: 'from-cyan-400 to-sky-500',
  indigo: 'from-indigo-400 to-blue-600',
  emerald: 'from-emerald-400 to-teal-600',
  amber: 'from-amber-400 to-orange-500',
  slate: 'from-slate-400 to-slate-600',
} as const;

export type KpiAccentBar = keyof typeof BAR;

export function KpiCard({
  label,
  value,
  hint,
  accent,
  accentBar = 'sky',
}: {
  label: string;
  value: string | number;
  hint?: string;
  accent?: string;
  accentBar?: KpiAccentBar;
}) {
  const barGradient = BAR[accentBar] ?? BAR.sky;

  return (
    <Card
      className={cn(
        'relative isolate overflow-hidden border-sky-100/90 bg-gradient-to-br from-white via-sky-50/40 to-white',
        'before:pointer-events-none before:absolute before:inset-x-0 before:top-0 before:h-[3px]',
        'before:bg-gradient-to-r before:from-sky-400/70 before:via-cyan-300/80 before:to-sky-500/70',
      )}
    >
      <div
        className={cn('absolute left-0 top-3 bottom-3 w-[3px] rounded-full opacity-95', 'bg-gradient-to-b', barGradient)}
        aria-hidden
      />
      <CardHeader className="pb-2 pl-5">
        <CardTitle className={cn('text-xs font-semibold uppercase tracking-wide text-sky-800/65', accent)}>
          {label}
        </CardTitle>
      </CardHeader>
      <CardContent className="pt-0 pb-5 pl-5">
        <div className="text-2xl font-semibold tracking-tight text-[#0c4a6e] tabular-nums">{value}</div>
        {hint && (
          <div className="text-xs font-medium text-sky-700/55 mt-1.5">{hint}</div>
        )}
      </CardContent>
    </Card>
  );
}
