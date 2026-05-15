'use client';
import { useEffect, useState } from 'react';
import { Api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select } from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { fmtDate } from '@/lib/utils';
import { useUser } from '@/lib/user-context';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';
import { confirmPrompt } from '@/lib/confirm-action';
import { Check, CircleSlash, X } from 'lucide-react';

const STATUSES = ['pending', 'approved', 'rejected', 'suspended', 'needs_review'];

function companyPermissions(c: any) {
  const type = String(c?.companyType || '').toLowerCase();
  const approved = String(c?.onboardingStatus || '').toLowerCase() === 'approved';
  const isShipper = type === 'shipper';
  const isCarrier = type === 'carrier';
  return {
    canCreateLoads: isShipper,
    canSubmitClaims: isCarrier && approved,
    canAccessCarrierPortal: isCarrier && approved,
  };
}

export default function CompaniesPage() {
  const session = useUser();
  const canSetOnboarding = session?.user.role === 'admin';
  const [rows, setRows] = useState<any[]>([]);
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const [type, setType] = useState('');
  const [status, setStatus] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [searchQ, setSearchQ] = useState('');
  const pag = useClientPagination(rows, [type, status, searchQ]);

  useEffect(() => {
    const t = window.setTimeout(() => setSearchQ(searchInput.trim()), 350);
    return () => window.clearTimeout(t);
  }, [searchInput]);

  useEffect(() => {
    const params: Record<string, string> = {};
    if (type) params.type = type;
    if (status) params.status = status;
    if (searchQ) params.q = searchQ;
    let cancelled = false;
    Api.companies(params)
      .then((data) => {
        if (!cancelled) setRows(data);
      })
      .catch(() => {
        if (!cancelled) setRows([]);
      });
    return () => {
      cancelled = true;
    };
  }, [type, status, searchQ]);

  async function setSt(id: string, st: string, companyName: string) {
    const label = companyName || 'this company';
    const questions: Record<string, string> = {
      approved: `Approve onboarding for "${label}"?`,
      rejected: `Reject "${label}"?\n\nThey cannot post or claim loads until status changes.`,
      suspended: `Suspend "${label}"?\n\nTheir access may be blocked until status changes.`,
      needs_review: `Mark "${label}" as needing review?\n\nFollow up before final approval.`,
      pending: `Set "${label}" back to pending?`,
    };
    const confirmMsg = questions[st] || `Set "${label}" onboarding to "${st}"?`;
    if (!confirmPrompt(confirmMsg)) return;
    await Api.setCompanyStatus(id, st);
    const params: Record<string, string> = {};
    if (type) params.type = type;
    if (status) params.status = status;
    if (searchQ) params.q = searchQ;
    setRows(await Api.companies(params));
  }

  return (
    <Card>
      <CardHeader className="flex flex-col md:flex-row md:items-end gap-4">
        <CardTitle className="flex-1">Companies</CardTitle>
        <div className="space-y-1 min-w-[10rem] flex-1 max-w-md">
          <Label className="text-xs">Search</Label>
          <Input
            className="h-9"
            placeholder="Company name…"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && setSearchQ(searchInput.trim())}
          />
        </div>
        <div className="space-y-1">
          <Label className="text-xs">Type</Label>
          <Select value={type} onChange={(e) => setType(e.target.value)}>
            <option value="">All</option>
            <option value="shipper">Shipper</option>
            <option value="carrier">Carrier</option>
          </Select>
        </div>
        <div className="space-y-1">
          <Label className="text-xs">Status</Label>
          <Select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">All</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </Select>
        </div>
      </CardHeader>
      <CardContent>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="text-left text-muted-foreground border-b">
              <tr>
                <th className="py-2">Name</th>
                <th>Type</th>
                <th>Status</th>
                <th>Computed permissions</th>
                <th>MC</th>
                <th>DOT</th>
                <th>Created</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {pag.pageRows.map((c) => (
                <tr key={getId(c)} className="border-b last:border-0">
                  {(() => {
                    const p = companyPermissions(c);
                    return (
                      <>
                  <td className="py-2 font-medium">{c.name}</td>
                  <td className="capitalize">{c.companyType}</td>
                  <td className="capitalize">{c.onboardingStatus}</td>
                  <td className="text-xs">
                    <div className={p.canCreateLoads ? 'text-emerald-700' : 'text-muted-foreground'}>
                      Create loads: {p.canCreateLoads ? 'Yes' : 'No'}
                    </div>
                    <div className={p.canSubmitClaims ? 'text-emerald-700' : 'text-muted-foreground'}>
                      Submit claims: {p.canSubmitClaims ? 'Yes' : 'No'}
                    </div>
                    <div className={p.canAccessCarrierPortal ? 'text-emerald-700' : 'text-muted-foreground'}>
                      Carrier portal: {p.canAccessCarrierPortal ? 'Yes' : 'No'}
                    </div>
                  </td>
                  <td>{c.mcNumber || '—'}</td>
                  <td>{c.dotNumber || '—'}</td>
                  <td>{fmtDate(c.createdAt)}</td>
                  <td className="text-right">
                    {canSetOnboarding ? (
                      <div className="flex gap-1 justify-end">
                        {c.onboardingStatus !== 'approved' && (
                          <Button
                            size="icon"
                            variant="default"
                            title="Approve company"
                            aria-label="Approve company"
                            onClick={() => setSt(getId(c), 'approved', c.name || '')}
                          >
                            <Check className="h-4 w-4" />
                          </Button>
                        )}
                        {c.onboardingStatus !== 'rejected' && (
                          <Button
                            size="icon"
                            variant="outline"
                            title="Reject company"
                            aria-label="Reject company"
                            onClick={() => setSt(getId(c), 'rejected', c.name || '')}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        )}
                        {c.onboardingStatus !== 'suspended' && (
                          <Button
                            size="icon"
                            variant="ghost"
                            title="Suspend company"
                            aria-label="Suspend company"
                            onClick={() => setSt(getId(c), 'suspended', c.name || '')}
                          >
                            <CircleSlash className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    ) : (
                      <span className="text-xs text-muted-foreground">View only</span>
                    )}
                  </td>
                      </>
                    );
                  })()}
                </tr>
              ))}
              {pag.total === 0 && (
                <tr>
                  <td colSpan={8} className="py-8 text-center text-muted-foreground">
                    No companies match filters.
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
  );
}
