'use client';
import { useEffect, useState } from 'react';
import { Api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { StatusBadge } from '@/components/status-badge';
import { fmtDate, fmtMoney } from '@/lib/utils';
import { useUser } from '@/lib/user-context';
import {
  canAccessCarrierPortal,
  canAdvanceCarrierLeg,
  canSubmitClaims,
  isAdmin,
} from '@/lib/permissions';
import { useSocket } from '@/lib/use-socket';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';
import { Loader2, RefreshCw } from 'lucide-react';

export default function PortalPage() {
  const session = useUser();
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const [rows, setRows] = useState<any[]>([]);
  const [assignmentRows, setAssignmentRows] = useState<any[]>([]);
  const [statusBusyId, setStatusBusyId] = useState<string | null>(null);
  const [filter, setFilter] = useState({
    origin: '',
    destination: '',
    originId: '',
    destinationId: '',
    loadTypeId: '',
  });
  const [loadTypeOptions, setLoadTypeOptions] = useState<Array<{ id: number; name: string }>>([]);
  const [originOptions, setOriginOptions] = useState<Array<{ id: number; city: string; stateCode: string }>>([]);
  const [destinationOptions, setDestinationOptions] = useState<Array<{ id: number; city: string; stateCode: string }>>([]);
  const [target, setTarget] = useState<any | null>(null);
  const [claimType, setClaimType] = useState<'claim' | 'bid'>('claim');
  const [bidAmount, setBidAmount] = useState<number | ''>('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const pag = useClientPagination(rows, [rows]);
  const pagAssign = useClientPagination(assignmentRows, [assignmentRows]);

  async function reload() {
    const q: Record<string, string> = {};
    if (filter.originId) q.originId = filter.originId;
    if (filter.destinationId) q.destinationId = filter.destinationId;
    if (filter.loadTypeId) q.loadTypeId = filter.loadTypeId;
    const all = await Api.loadsList(q);
    setRows(all.filter((l) => String(l.status || '').toLowerCase() === 'posted'));
    setAssignmentRows(
      all.filter((l) => {
        const s = String(l.status || '').toLowerCase();
        return (
          !!l.assignedCarrierUserId &&
          ['assigned', 'claimed', 'in_transit', 'delivered'].includes(s)
        );
      }),
    );
  }
  useEffect(() => {
    reload().catch(() => {});
  }, []);

  useEffect(() => {
    Api.loadTypes().then(setLoadTypeOptions).catch(() => setLoadTypeOptions([]));
  }, []);

  useEffect(() => {
    const t = setTimeout(() => {
      const q = filter.origin.trim();
      if (!q) return setOriginOptions([]);
      Api.locationsPlaces(q, 20)
        .then((rows: any[]) => {
          const vals = rows
            .map((r) => ({
              id: Number(r.id || 0),
              city: String(r.city || '').trim(),
              stateCode: String(r.stateCode || '').trim(),
            }))
            .filter((r) => r.id > 0 && r.city);
          setOriginOptions(vals);
        })
        .catch(() => setOriginOptions([]));
    }, 160);
    return () => clearTimeout(t);
  }, [filter.origin]);

  useEffect(() => {
    const t = setTimeout(() => {
      const q = filter.destination.trim();
      if (!q) return setDestinationOptions([]);
      Api.locationsPlaces(q, 20)
        .then((rows: any[]) => {
          const vals = rows
            .map((r) => ({
              id: Number(r.id || 0),
              city: String(r.city || '').trim(),
              stateCode: String(r.stateCode || '').trim(),
            }))
            .filter((r) => r.id > 0 && r.city);
          setDestinationOptions(vals);
        })
        .catch(() => setDestinationOptions([]));
    }, 160);
    return () => clearTimeout(t);
  }, [filter.destination]);

  function clearFilters() {
    setFilter({ origin: '', destination: '', originId: '', destinationId: '', loadTypeId: '' });
    setOriginOptions([]);
    setDestinationOptions([]);
    Api.loadsList({}).then((all) => {
      setRows(all.filter((l) => String(l.status || '').toLowerCase() === 'posted'));
      setAssignmentRows(
        all.filter((l) => {
          const s = String(l.status || '').toLowerCase();
          return !!l.assignedCarrierUserId && ['assigned', 'claimed', 'in_transit', 'delivered'].includes(s);
        }),
      );
    });
  }
  const hasAnyFilter = !!(
    filter.origin.trim() ||
    filter.destination.trim() ||
    filter.originId ||
    filter.destinationId ||
    filter.loadTypeId
  );

  async function setAssignmentStatus(loadId: string, status: string) {
    setStatusBusyId(loadId);
    try {
      await Api.setLoadStatus(loadId, status);
      await reload();
    } catch (e: any) {
      alert(e?.message || 'Cannot update status');
    } finally {
      setStatusBusyId(null);
    }
  }
  async function manualRefresh() {
    setRefreshing(true);
    try {
      await reload();
    } finally {
      setRefreshing(false);
    }
  }
  useSocket((ev) => {
    if (
      ev === 'load_updated' ||
      ev === 'load_assigned' ||
      ev === 'claim_accepted' ||
      ev === 'claim_rejected' ||
      ev === 'claim_updated'
    )
      reload().catch(() => {});
  });

  async function submit() {
    if (!target) return;
    setError('');
    setSubmitting(true);
    try {
      await Api.submitClaim({
        loadId: Number(getId(target)),
        claimType,
        bidAmount: claimType === 'bid' ? Number(bidAmount || 0) : undefined,
        message,
      });
      setTarget(null);
      setBidAmount('');
      setMessage('');
      await reload();
    } catch (err: any) {
      setError(err.message || 'Submission failed');
    } finally {
      setSubmitting(false);
    }
  }

  const companyTypeLc = String(session?.company?.companyType || '').toLowerCase();
  const onboardOk = String(session?.company?.onboardingStatus || '').toLowerCase() === 'approved';
  const carrierCompany = companyTypeLc === 'carrier';
  const canUsePortal = canAccessCarrierPortal(session?.user as any, session?.company);
  const canActAsCarrier = canSubmitClaims(session?.user as any, session?.company);
  const canClaimOrBid =
    isAdmin(session?.user as any) ||
    (canActAsCarrier && carrierCompany && onboardOk);

  if (!canUsePortal) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Carrier portal</CardTitle>
        </CardHeader>
        <CardContent className="py-8 text-sm text-muted-foreground">
          This area is for carrier companies and dispatchers who work for a carrier. Shipper-side dispatchers should use{' '}
          <strong>Loads</strong> to create and manage freight.
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {canActAsCarrier && carrierCompany && !onboardOk && !isAdmin(session?.user as any) && (
        <Card>
          <CardContent className="py-6 text-sm text-amber-700">
            Your carrier company is not approved yet. Contact admin to enable claiming and bidding.
          </CardContent>
        </Card>
      )}

      {canActAsCarrier && onboardOk && (
        <Card>
          <CardHeader className="flex flex-row items-start justify-between gap-2">
            <div>
              <CardTitle>Your assignments — workflow</CardTitle>
            <p className="text-sm text-muted-foreground font-normal pt-1">
              After a load is assigned to you (or your company), move it <strong>In transit</strong> when the haul starts,
              then <strong>Delivered</strong> when unloading is finished. An <strong>administrator</strong> marks{' '}
              <strong>Completed</strong> on Loads after delivery.
            </p>
            </div>
            <Button type="button" variant="outline" size="sm" onClick={manualRefresh} disabled={refreshing}>
              <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            </Button>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="text-left text-muted-foreground border-b">
                  <tr>
                    <th className="py-2">Ref</th>
                    <th>Origin → Destination</th>
                    <th>Status</th>
                    <th className="text-right">Update status</th>
                  </tr>
                </thead>
                <tbody>
                  {pagAssign.total === 0 ? (
                    <tr>
                      <td colSpan={4} className="py-8 text-center text-muted-foreground">
                        No assigned loads yet. When an admin accepts your claim or bid, the haul appears here for status
                        updates.
                      </td>
                    </tr>
                  ) : (
                    pagAssign.pageRows.map((l) => {
                      const st = String(l.status || '').toLowerCase();
                      const busy = statusBusyId === getId(l);
                      const canLeg = canAdvanceCarrierLeg(session?.user as any, l, session?.company);
                      return (
                        <tr key={getId(l)} className="border-b last:border-0">
                          <td className="py-2 font-medium">{l.refId || getId(l).slice(-6)}</td>
                          <td>
                            {l.origin?.city}, {l.origin?.state} → {l.destination?.city},{' '}
                            {l.destination?.state}
                          </td>
                          <td>
                            <StatusBadge status={l.status} />
                          </td>
                          <td className="text-right">
                            <div className="flex flex-wrap gap-1 justify-end">
                              {canLeg && ['assigned', 'claimed'].includes(st) && (
                                <Button
                                  size="sm"
                                  variant="secondary"
                                  disabled={busy}
                                  onClick={() => setAssignmentStatus(getId(l), 'in_transit')}
                                >
                                  In transit
                                </Button>
                              )}
                              {canLeg && st === 'in_transit' && (
                                <Button
                                  size="sm"
                                  variant="secondary"
                                  disabled={busy}
                                  onClick={() => setAssignmentStatus(getId(l), 'delivered')}
                                >
                                  Delivered
                                </Button>
                              )}
                              {st === 'delivered' && (
                                <span className="text-xs text-muted-foreground inline-block py-2">
                                  Awaiting admin completed
                                </span>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
            <TablePagination
              page={pagAssign.page}
              totalPages={pagAssign.totalPages}
              pageSize={pagAssign.pageSize}
              total={pagAssign.total}
              showingFrom={pagAssign.showingFrom}
              showingTo={pagAssign.showingTo}
              onPageChange={pagAssign.setPage}
              onPageSizeChange={(n) => {
                pagAssign.setPageSize(n);
                pagAssign.setPage(1);
              }}
            />
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader className="space-y-4">
          <div className="flex items-start justify-between gap-2">
            <div>
              <CardTitle>Available loads</CardTitle>
              <p className="text-sm text-muted-foreground pt-1">
                Use filters below, then click <strong>Search</strong> to apply exact dropdown filters.
              </p>
            </div>
            <Button type="button" variant="outline" size="sm" onClick={manualRefresh} disabled={refreshing}>
              <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            </Button>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3">
            <div className="space-y-1">
              <Label className="text-xs">Origin city</Label>
              <Input
                className="h-9"
                value={filter.origin}
                list="portal-origin-city-list"
                onChange={(e) => {
                  const v = e.target.value;
                  const match = originOptions.find(
                    (o) =>
                      `${o.city}, ${o.stateCode}`.toLowerCase() === v.trim().toLowerCase() ||
                      o.city.toLowerCase() === v.trim().toLowerCase(),
                  );
                  setFilter((p) => ({ ...p, origin: v, originId: match ? String(match.id) : '' }));
                }}
              />
              <datalist id="portal-origin-city-list">
                {originOptions.map((c) => (
                  <option key={c.id} value={`${c.city}, ${c.stateCode}`} />
                ))}
              </datalist>
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Destination city</Label>
              <Input
                className="h-9"
                value={filter.destination}
                list="portal-destination-city-list"
                onChange={(e) => {
                  const v = e.target.value;
                  const match = destinationOptions.find(
                    (o) =>
                      `${o.city}, ${o.stateCode}`.toLowerCase() === v.trim().toLowerCase() ||
                      o.city.toLowerCase() === v.trim().toLowerCase(),
                  );
                  setFilter((p) => ({ ...p, destination: v, destinationId: match ? String(match.id) : '' }));
                }}
              />
              <datalist id="portal-destination-city-list">
                {destinationOptions.map((c) => (
                  <option key={c.id} value={`${c.city}, ${c.stateCode}`} />
                ))}
              </datalist>
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Equipment type</Label>
              <Select
                className="h-9"
                value={filter.loadTypeId}
                onChange={(e) => setFilter((p) => ({ ...p, loadTypeId: e.target.value }))}
              >
                <option value="">All</option>
                {loadTypeOptions.map((lt) => (
                  <option key={lt.id} value={String(lt.id)}>
                    {lt.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="flex items-end gap-2">
              <Button type="button" onClick={reload} className="h-9 px-5 flex-1 sm:flex-none">
                Search
              </Button>
              {hasAnyFilter && (
                <Button
                  type="button"
                  variant="outline"
                  className="h-9 px-5 flex-1 sm:flex-none"
                  onClick={clearFilters}
                >
                  Clear
                </Button>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="text-left text-muted-foreground border-b">
                <tr>
                  <th className="py-2">Ref</th>
                  <th>Origin → Destination</th>
                  <th>Equipment</th>
                  <th>Pickup</th>
                  <th>Status</th>
                  <th className="text-right">Posted rate</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {pag.pageRows.map((l) => (
                  <tr key={getId(l)} className="border-b last:border-0">
                    <td className="py-2 font-medium">{l.refId || getId(l).slice(-6)}</td>
                    <td>
                      {l.origin?.city}, {l.origin?.state} → {l.destination?.city}, {l.destination?.state}
                    </td>
                    <td>
                      {l.equipmentType} · {l.trailerLengthFt}ft
                    </td>
                    <td>{fmtDate(l.pickUpDate)}</td>
                    <td>
                      <StatusBadge status={l.status} />
                    </td>
                    <td className="text-right">{fmtMoney(l.billedToCustomer)}</td>
                    <td className="text-right">
                      <Button
                        size="sm"
                        disabled={!canClaimOrBid}
                        onClick={() => {
                          setTarget(l);
                          setClaimType('claim');
                        }}
                      >
                        Claim / Bid
                      </Button>
                    </td>
                  </tr>
                ))}
                {pag.total === 0 && (
                  <tr>
                    <td colSpan={7} className="py-8 text-center text-muted-foreground">
                      No posted loads match your filters.
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

      <Dialog open={!!target} onOpenChange={(o) => !o && setTarget(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Submit {claimType}</DialogTitle>
          </DialogHeader>
          {target && (
            <div className="space-y-3 text-sm">
              <div className="rounded-md bg-muted p-2">
                <div className="font-medium">{target.refId || getId(target)}</div>
                <div>
                  {target.origin?.city}, {target.origin?.state} → {target.destination?.city},{' '}
                  {target.destination?.state}
                </div>
                <div className="text-muted-foreground">
                  Posted rate: {fmtMoney(target.billedToCustomer)}
                </div>
              </div>
              <div className="space-y-1">
                <Label>Type</Label>
                <Select value={claimType} onChange={(e) => setClaimType(e.target.value as any)}>
                  <option value="claim">Claim posted rate</option>
                  <option value="bid">Submit bid</option>
                </Select>
              </div>
              {claimType === 'bid' && (
                <div className="space-y-1">
                  <Label>Bid amount (USD)</Label>
                  <Input
                    type="number"
                    value={bidAmount}
                    onChange={(e) => setBidAmount(e.target.value ? Number(e.target.value) : '')}
                  />
                </div>
              )}
              <div className="space-y-1">
                <Label>Message (optional)</Label>
                <Textarea value={message} onChange={(e) => setMessage(e.target.value)} />
              </div>
              {error && <div className="text-sm text-destructive">{error}</div>}
              <Button onClick={submit} className="w-full" disabled={submitting}>
                {submitting ? (
                  <span className="inline-flex items-center gap-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Submitting…
                  </span>
                ) : (
                  'Submit'
                )}
              </Button>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
