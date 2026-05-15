'use client';

import { useEffect, useMemo, useState } from 'react';

export const DEFAULT_PAGE_SIZE = 10;

export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

export type ClientPagination<T> = {
  pageRows: T[];
  page: number;
  setPage: (p: number) => void;
  pageSize: number;
  setPageSize: (n: number) => void;
  total: number;
  totalPages: number;
  showingFrom: number;
  showingTo: number;
};

/** Client-side slice of `items`; resets current page when `resetDeps` change (e.g. filters). */
export function useClientPagination<T>(
  items: readonly T[],
  resetDeps: ReadonlyArray<unknown> = [],
): ClientPagination<T> {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);

  useEffect(() => {
    setPage(1);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- caller supplies reset deps explicitly
  }, resetDeps);

  const total = items.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize || 1));
  const safePage = Math.min(Math.max(1, page), totalPages);

  const { pageRows, showingFrom, showingTo } = useMemo(() => {
    if (total === 0) {
      return { pageRows: [] as T[], showingFrom: 0, showingTo: 0 };
    }
    const start = (safePage - 1) * pageSize;
    const slice = items.slice(start, start + pageSize) as T[];
    return {
      pageRows: slice,
      showingFrom: start + 1,
      showingTo: start + slice.length,
    };
  }, [items, safePage, pageSize, total]);

  return {
    pageRows,
    page: safePage,
    setPage,
    pageSize,
    setPageSize,
    total,
    totalPages,
    showingFrom,
    showingTo,
  };
}
