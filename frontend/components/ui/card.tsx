import * as React from 'react';
import { cn } from '@/lib/utils';

export const Card = ({ className, ...p }: React.HTMLAttributes<HTMLDivElement>) => (
  <div
    className={cn(
      'rounded-xl border border-sky-100/80 bg-white/90 backdrop-blur-sm shadow-[0_10px_30px_-14px_rgba(2,132,199,0.35)]',
      className,
    )}
    {...p}
  />
);
export const CardHeader = ({ className, ...p }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('flex flex-col gap-1 p-5', className)} {...p} />
);
export const CardTitle = ({ className, ...p }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('text-base font-semibold tracking-tight', className)} {...p} />
);
export const CardDescription = ({ className, ...p }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('text-sm text-muted-foreground', className)} {...p} />
);
export const CardContent = ({ className, ...p }: React.HTMLAttributes<HTMLDivElement>) => (
  <div className={cn('p-5 pt-0', className)} {...p} />
);
