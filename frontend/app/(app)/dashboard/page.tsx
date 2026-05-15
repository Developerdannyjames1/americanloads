'use client';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { KpiCard } from '@/components/kpi-card';
import { ProfitDonut } from '@/components/profit-donut';
import { StatusBadge } from '@/components/status-badge';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';
import { fmtMoney, fmtPercent, fmtDate } from '@/lib/utils';

export default function DashboardPage() {
  const [kpis, setKpis] = useState<any>(null);
  const [allLoadsForTable, setAllLoadsForTable] = useState<any[]>([]);
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const byStatus = kpis?.byStatus || kpis?.countsByStatus || {};
  const totals = kpis?.totals || {};
  const totalLoads = totals.loads ?? kpis?.totalLoads ?? 0;
  const totalRevenue = totals.revenue ?? kpis?.totalRevenue ?? 0;
  const totalCost = totals.cost ?? kpis?.totalCarrierCost ?? 0;
  const totalProfit = totals.profit ?? kpis?.totalProfit ?? 0;
  const marginPercent = totals.marginPercent ?? kpis?.marginPercent ?? 0;
  const pendingClaims = kpis?.pendingClaims ?? kpis?.claimsPending ?? 0;

  const pag = useClientPagination(allLoadsForTable, []);

  useEffect(() => {
    Api.kpis().then(setKpis).catch(() => {});
    Api.loadsList()
      .then((rows) => setAllLoadsForTable(rows))
      .catch(() => {});
  }, []);

  if (!kpis) return <div className="text-muted-foreground">Loading metrics…</div>;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
        <KpiCard label="Total loads" value={totalLoads} accentBar="sky" />
        <KpiCard label="Posted" value={byStatus.posted || 0} accentBar="cyan" />
        <KpiCard label="In transit" value={byStatus.in_transit || 0} accentBar="indigo" />
        <KpiCard label="Completed" value={byStatus.completed || 0} accentBar="emerald" />
        <KpiCard label="Pending claims" value={pendingClaims} accentBar="amber" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Financial summary</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <KpiCard label="Revenue" value={fmtMoney(totalRevenue)} hint="Billed to customer" accentBar="sky" />
            <KpiCard label="Carrier cost" value={fmtMoney(totalCost)} hint="Pay to carrier" accentBar="slate" />
            <KpiCard
              label="Profit"
              value={fmtMoney(totalProfit)}
              hint={`${fmtPercent(marginPercent)} margin`}
              accent="text-emerald-600"
              accentBar="emerald"
            />
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Profit vs Carrier cost</CardTitle>
          </CardHeader>
          <CardContent>
            <ProfitDonut revenue={totalRevenue} cost={totalCost} />
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Recent loads</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="text-left text-muted-foreground border-b">
                <tr>
                  <th className="py-2">Ref</th>
                  <th>Origin</th>
                  <th>Destination</th>
                  <th>Equipment</th>
                  <th>Pickup</th>
                  <th>Status</th>
                  <th className="text-right">Revenue</th>
                  <th className="text-right">Profit</th>
                </tr>
              </thead>
              <tbody>
                {pag.pageRows.map((l) => (
                  <tr key={getId(l)} className="border-b last:border-0">
                    <td className="py-2 font-medium">
                      <Link href={`/loads`} className="hover:underline">
                        {l.refId || getId(l).slice(-6)}
                      </Link>
                    </td>
                    <td>
                      {l.origin?.city}, {l.origin?.state}
                    </td>
                    <td>
                      {l.destination?.city}, {l.destination?.state}
                    </td>
                    <td>{l.equipmentType}</td>
                    <td>{fmtDate(l.pickUpDate)}</td>
                    <td>
                      <StatusBadge status={l.status} />
                    </td>
                    <td className="text-right">{fmtMoney(l.billedToCustomer)}</td>
                    <td className="text-right text-emerald-600">{fmtMoney(l.profit)}</td>
                  </tr>
                ))}
                {pag.total === 0 && (
                  <tr>
                    <td colSpan={8} className="py-8 text-center text-muted-foreground">
                      No loads yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <TablePagination
            page={pag.page}
            totalPages={pag.totalPages}
            pageSize={pag.pageSize}
            total={pag.total}
            showingFrom={pag.showingFrom}
            showingTo={pag.showingTo}
            onPageChange={pag.setPage}
            onPageSizeChange={(n) => {
              pag.setPageSize(n);
              pag.setPage(1);
            }}
          />
        </CardContent>
      </Card>
    </div>
  );
}
