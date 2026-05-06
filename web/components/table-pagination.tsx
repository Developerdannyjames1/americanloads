'use client';

import { Button } from '@/components/ui/button';
import { Select } from '@/components/ui/select';
import { PAGE_SIZE_OPTIONS } from '@/lib/use-client-pagination';

export type TablePaginationProps = {
  page: number;
  totalPages: number;
  pageSize: number;
  total: number;
  showingFrom: number;
  showingTo: number;
  onPageChange: (p: number) => void;
  onPageSizeChange: (n: number) => void;
};

export function TablePagination({
  page,
  totalPages,
  pageSize,
  total,
  showingFrom,
  showingTo,
  onPageChange,
  onPageSizeChange,
}: TablePaginationProps) {
  const canPrev = page > 1;
  const canNext = page < totalPages;

  return (
    <div className="flex flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-3 pt-4 mt-4 border-t text-sm text-muted-foreground">
      <div>
        {total === 0 ? (
          <>No rows</>
        ) : (
          <>
            Showing <span className="text-foreground">{showingFrom}</span>–
            <span className="text-foreground">{showingTo}</span> of{' '}
            <span className="text-foreground">{total}</span>
          </>
        )}
      </div>
      <div className="flex flex-wrap items-center gap-2 justify-end">
        <div className="flex items-center gap-2">
          <span className="hidden sm:inline whitespace-nowrap">Rows</span>
          <Select
            className="h-9 w-[110px]"
            value={String(pageSize)}
            onChange={(e) => {
              const n = Number(e.target.value);
              onPageSizeChange(n);
            }}
          >
            {PAGE_SIZE_OPTIONS.map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </Select>
        </div>
        <div className="flex items-center gap-1">
          <Button type="button" variant="outline" size="sm" disabled={!canPrev} onClick={() => onPageChange(page - 1)}>
            Previous
          </Button>
          <span className="px-2 min-w-[4.5rem] text-center text-foreground tabular-nums">
            {total === 0 ? '—' : `${page} / ${totalPages}`}
          </span>
          <Button type="button" variant="outline" size="sm" disabled={!canNext} onClick={() => onPageChange(page + 1)}>
            Next
          </Button>
        </div>
      </div>
    </div>
  );
}
