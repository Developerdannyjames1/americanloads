'use client';
import { useEffect, useState } from 'react';
import { Api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { LoadForm, type LoadFormValues } from '@/components/load-form';
import { fmtDate, fmtMoney } from '@/lib/utils';
import { useUser } from '@/lib/user-context';
import { canAcceptRejectClaims, canSetCarrierPay, canSubmitClaims, isAdmin } from '@/lib/permissions';
import { useSocket } from '@/lib/use-socket';
import { confirmPrompt } from '@/lib/confirm-action';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';
import { Check, DollarSign, Loader2, RefreshCw, X } from 'lucide-react';

export default function ClaimsPage() {
  const session = useUser();
  const [items, setItems] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [editingLoad, setEditingLoad] = useState<any | null>(null);
  const [paySaving, setPaySaving] = useState(false);
  const [payError, setPayError] = useState('');
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const pag = useClientPagination(items, []);

  async function reload() {
    setLoading(true);
    try {
      if (canSubmitClaims(session?.user as any, session?.company)) {
        const list = await Api.myClaims();
        setItems(list);
        return;
      }
      // Admin / shipper: loads visible to the user, then claims per load (Step 4 for admin).
      const loads = await Api.loadsList({});
      const allClaims: any[] = [];
      for (const l of loads) {
        try {
          const cs = await Api.claimsForLoad(getId(l));
          cs.forEach((c: any) => allClaims.push({ ...c, load: l }));
        } catch {}
      }
      setItems(allClaims);
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    if (session) reload().catch(() => {});
  }, [session]);

  useSocket((ev) => {
    if (
      ev === 'new_claim' ||
      ev === 'new_bid' ||
      ev === 'claim_updated' ||
      ev === 'load_assigned' ||
      ev === 'load_updated'
    )
      reload().catch(() => {});
  });

  async function accept(id: string) {
    if (!confirmPrompt('Accept this claim or bid?\n\nThis will update the load and notify parties.')) return;
    await Api.acceptClaim(id);
    await reload();
  }
  async function reject(id: string) {
    if (!confirmPrompt('Reject this claim or bid?\n\nThis cannot be undone.')) return;
    await Api.rejectClaim(id);
    await reload();
  }

  async function openPayCarrierModal(c: any) {
    const lid = String(c.loadId ?? c.load?.id ?? c.load?._id ?? '');
    if (!lid) return;
    setPayError('');
    try {
      const full = await Api.loadById(lid);
      setEditingLoad(full);
    } catch (e: any) {
      setPayError(e?.message || 'Could not open load');
    }
  }

  async function onSaveLoadFromClaims(v: LoadFormValues) {
    if (!editingLoad) return;
    setPaySaving(true);
    setPayError('');
    try {
      await Api.updateLoad(getId(editingLoad), v);
      setPayError('');
      setEditingLoad(null);
      await reload();
    } catch (e: any) {
      setPayError(e?.message || 'Save failed');
    } finally {
      setPaySaving(false);
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

  const staff = canAcceptRejectClaims(session?.user as any, session?.company);
  const showAdminActions = isAdmin(session?.user as any);
  const canPayCarrier = canSetCarrierPay(session?.user as any);

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div>
          <CardTitle>Claims & Bids</CardTitle>
        <p className="text-sm text-muted-foreground font-normal pt-1">
          Shippers can review submissions below; only <strong>administrators</strong> see Accept, Reject, and Pay carrier (opens the same load editor as Loads, including carrier pay). Accept assigns the carrier and may apply a bid to carrier pay; Reject notifies the carrier.
        </p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={manualRefresh} disabled={refreshing || loading}>
          <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
        </Button>
      </CardHeader>
      <CardContent>
        {payError && !editingLoad && (
          <div className="text-sm text-destructive mb-3" role="alert">
            {payError}
          </div>
        )}
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="text-left text-muted-foreground border-b">
              <tr>
                <th className="py-2">Date</th>
                <th>Load</th>
                <th>Type</th>
                <th>Bid</th>
                <th>Status</th>
                <th>Message</th>
                {showAdminActions && <th className="text-right">Actions</th>}
              </tr>
            </thead>
            <tbody>
              {pag.pageRows.map((c) => (
                <tr key={getId(c)} className="border-b last:border-0">
                  <td className="py-2">{fmtDate(c.createdAt)}</td>
                  <td className="font-medium">
                    {c.load?.refId || c.loadId?.slice?.(-6) || c.loadId}
                  </td>
                  <td className="capitalize">{c.claimType}</td>
                  <td>{c.bidAmount ? fmtMoney(c.bidAmount) : '—'}</td>
                  <td className="capitalize">{c.status}</td>
                  <td className="text-muted-foreground max-w-[300px] truncate">{c.message}</td>
                  {showAdminActions && (
                    <td className="text-right">
                      <div className="flex flex-wrap gap-1 justify-end">
                        {canPayCarrier && (
                          <Button
                            size="icon"
                            variant="outline"
                            title="Pay carrier"
                            aria-label="Pay carrier"
                            onClick={() => openPayCarrierModal(c)}
                          >
                            <DollarSign className="h-3.5 w-3.5" />
                          </Button>
                        )}
                        {staff && c.status === 'pending' && (
                          <>
                            <Button
                              size="icon"
                              title="Accept claim/bid"
                              aria-label="Accept claim/bid"
                              onClick={() => accept(getId(c))}
                            >
                              <Check className="h-4 w-4" />
                            </Button>
                            <Button
                              size="icon"
                              variant="outline"
                              title="Reject claim/bid"
                              aria-label="Reject claim/bid"
                              onClick={() => reject(getId(c))}
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          </>
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
              {loading && (
                <tr>
                  <td colSpan={showAdminActions ? 7 : 6} className="py-10 text-center text-muted-foreground">
                    <span className="inline-flex items-center gap-2">
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Loading claims…
                    </span>
                  </td>
                </tr>
              )}
              {!loading && pag.total === 0 && (
                <tr>
                  <td colSpan={showAdminActions ? 7 : 6} className="py-8 text-center text-muted-foreground">
                    No claims yet.
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

      <Dialog
        open={!!editingLoad}
        onOpenChange={(open) => {
          if (!open) {
            setEditingLoad(null);
            setPayError('');
          }
        }}
      >
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit load — set carrier pay</DialogTitle>
          </DialogHeader>
          {editingLoad && (
            <p className="text-sm text-muted-foreground -mt-2">
              Ref: {(editingLoad.refId || '').trim() || getId(editingLoad)} · Same form as Loads; save applies carrier pay and any other edits.
            </p>
          )}
          {payError && <div className="text-sm text-destructive">{payError}</div>}
          {editingLoad && (
            <LoadForm
              key={getId(editingLoad)}
              initial={editingLoad}
              onSubmit={onSaveLoadFromClaims}
              onCancel={() => setEditingLoad(null)}
              saving={paySaving}
              submitLabel="Save changes"
            />
          )}
        </DialogContent>
      </Dialog>
    </Card>
  );
}
