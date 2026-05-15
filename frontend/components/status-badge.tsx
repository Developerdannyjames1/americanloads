import { Badge } from '@/components/ui/badge';
import { STATUS_COLORS } from '@/lib/constants';
import { cn } from '@/lib/utils';

export function StatusBadge({ status }: { status?: string }) {
  const s = (status || 'draft').toString();
  const cls = STATUS_COLORS[s] || 'bg-slate-100 text-slate-700';
  return <Badge className={cn(cls)}>{s.replaceAll('_', ' ')}</Badge>;
}
