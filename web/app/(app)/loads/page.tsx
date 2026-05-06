'use client';
import { useEffect, useState } from 'react';
import { Api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { StatusBadge } from '@/components/status-badge';
import { LoadForm, type LoadFormValues } from '@/components/load-form';
import { fmtDate, fmtMoney } from '@/lib/utils';
import { LOAD_STATUS } from '@/lib/constants';
import { useUser } from '@/lib/user-context';
import {
  canCancelLoad,
  canCreateLoads,
  canMarkLoadCompleted,
  canUseCarrierLegWorkflow,
  isAdmin,
} from '@/lib/permissions';
import { confirmDelete, confirmDuplicate } from '@/lib/confirm-action';
import { Copy, Pencil, Plus, RefreshCw, Trash2 } from 'lucide-react';
import { useSocket } from '@/lib/use-socket';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';

export default function LoadsPage() {
  const session = useUser();
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const [rows, setRows] = useState<any[]>([]);
  const [filter, setFilter] = useState({
    status: '',
    q: '',
    origin: '',
    destination: '',
    originId: '',
    destinationId: '',
    loadTypeId: '',
  });
  const [openCreate, setOpenCreate] = useState(false);
  const [editing, setEditing] = useState<any | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusBusy, setStatusBusy] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [createError, setCreateError] = useState('');
  const [editError, setEditError] = useState('');
  const [shipperCompanyOptions, setShipperCompanyOptions] = useState<Array<{ id: number; name: string }>>([]);
  const [shipperCompanyLoading, setShipperCompanyLoading] = useState(false);
  const [loadTypeOptions, setLoadTypeOptions] = useState<Array<{ id: number; name: string }>>([]);
  const [loadTypeLoading, setLoadTypeLoading] = useState(false);
  const [originOptions, setOriginOptions] = useState<Array<{ id: number; city: string; stateCode: string }>>([]);
  const [destinationOptions, setDestinationOptions] = useState<Array<{ id: number; city: string; stateCode: string }>>([]);

  async function reload() {
    const q: Record<string, string> = {};
    if (filter.status) q.status = filter.status;
    if (filter.q.trim()) q.refId = filter.q.trim();
    if (filter.originId) q.originId = filter.originId;
    if (filter.destinationId) q.destinationId = filter.destinationId;
    if (filter.loadTypeId) q.loadTypeId = filter.loadTypeId;
    const list = await Api.loadsList(q);
    setRows(list);
  }
  const pag = useClientPagination(rows, [rows]);

  useEffect(() => {
    reload().catch(() => {});
  }, []);

  useEffect(() => {
    setLoadTypeLoading(true);
    Api.loadTypes()
      .then(setLoadTypeOptions)
      .catch(() => setLoadTypeOptions([]))
      .finally(() => setLoadTypeLoading(false));
  }, []);

  useEffect(() => {
    if (!(openCreate || !!editing) || !session?.user) {
      setShipperCompanyOptions([]);
      setShipperCompanyLoading(false);
      return;
    }
    const role = session.user.role;
    const ct = String(session.company?.companyType || '').toLowerCase();
    if (role === 'admin') {
      setShipperCompanyLoading(true);
      Api.companies({ type: 'shipper', status: 'approved' })
        .then((rows: any[]) =>
          setShipperCompanyOptions(
            rows.map((r) => ({ id: Number(r.id), name: String(r.name || `Company #${r.id}`) })),
          ),
        )
        .catch(() => setShipperCompanyOptions([]))
        .finally(() => setShipperCompanyLoading(false));
      return;
    }
    if ((role === 'shipper' || role === 'dispatcher') && ct === 'shipper' && session.company?.id) {
      setShipperCompanyOptions([{ id: Number(session.company.id), name: session.company.name }]);
      setShipperCompanyLoading(false);
      return;
    }
    setShipperCompanyOptions([]);
    setShipperCompanyLoading(false);
  }, [openCreate, editing, session]);

  useSocket((ev) => {
    if (ev === 'load_updated' || ev === 'load_assigned') reload().catch(() => {});
  });

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
    setFilter({ status: '', q: '', origin: '', destination: '', originId: '', destinationId: '', loadTypeId: '' });
    setOriginOptions([]);
    setDestinationOptions([]);
    Api.loadsList({}).then(setRows).catch(() => {});
  }
  const hasAnyFilter = !!(
    filter.status ||
    filter.q.trim() ||
    filter.origin.trim() ||
    filter.destination.trim() ||
    filter.originId ||
    filter.destinationId ||
    filter.loadTypeId ||
    false
  );

  async function onCreate(v: LoadFormValues) {
    setSaving(true);
    setCreateError('');
    try {
      await Api.createLoad(v);
      setOpenCreate(false);
      await reload();
    } catch (err: any) {
      setCreateError(err.message || 'Save failed');
    } finally {
      setSaving(false);
    }
  }
  async function onUpdate(v: LoadFormValues) {
    if (!editing) return;
    setSaving(true);
    setEditError('');
    try {
      await Api.updateLoad(getId(editing), v);
      setEditing(null);
      await reload();
    } catch (err: any) {
      setEditError(err.message || 'Save failed');
    } finally {
      setSaving(false);
    }
  }

  async function setLoadStatusInEditor(status: string) {
    if (!editing) return;
    const id = getId(editing);
    setStatusBusy(true);
    try {
      await Api.setLoadStatus(id, status);
      await reload();
      const fresh = await Api.loadById(id);
      setEditing(fresh);
    } catch (e: any) {
      alert(e?.message || 'Cannot set status');
    } finally {
      setStatusBusy(false);
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

  async function duplicateFromRow(l: any) {
    const label = (l.refId || '').trim() || `#${getId(l).slice(-8)}`;
    if (!confirmDuplicate({ detail: `Source: ${label}` })) return;
    try {
      const copy = await Api.duplicateLoad(getId(l));
      await reload();
      setEditError('');
      setEditing(copy);
    } catch (e: any) {
      alert(e?.message || 'Duplicate failed');
    }
  }

  function remove(id: string, label?: string) {
    if (!confirmDelete({ subject: 'this load', name: label })) return;
    Api.deleteLoad(id)
      .then(() => reload())
      .catch((e: any) => alert(e?.message || 'Delete failed'));
  }

  function openEdit(l: any) {
    setEditError('');
    setEditing(l);
  }

  /** Permission helpers expect the `user` object; `useUser()` returns `{ user, company }`. */
  const currentUser = session?.user ?? null;

  const workflowForEdit =
    editing && session && currentUser
      ? {
          canPost:
            editing.status === 'draft' &&
            (isAdmin(currentUser) ||
              (currentUser.role === 'shipper' && String(editing.shipperUserId) === currentUser.sub)),
          canExecCarrierLeg: canUseCarrierLegWorkflow(currentUser, editing, session.company),
          canMarkCompleted: canMarkLoadCompleted(currentUser),
          canCancel:
            canCancelLoad(currentUser, editing) &&
            editing.status !== 'cancelled' &&
            editing.status !== 'completed',
        }
      : null;

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-col gap-4">
          <CardTitle>Loads</CardTitle>
          <div className="flex flex-col gap-3 w-full">
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-2 items-end">
              <div className="space-y-1">
                <Label className="text-xs">Search ref</Label>
                <Input
                  className="h-9"
                  placeholder="Ref / PRO"
                  value={filter.q}
                  onChange={(e) => setFilter((p) => ({ ...p, q: e.target.value }))}
                  onKeyDown={(e) => e.key === 'Enter' && reload()}
                />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Status</Label>
                <Select
                  value={filter.status}
                  className="h-9"
                  onChange={(e) => setFilter((p) => ({ ...p, status: e.target.value }))}
                >
                  <option value="">All</option>
                  {LOAD_STATUS.map((s) => (
                    <option key={s} value={s}>
                      {s.replaceAll('_', ' ')}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Origin city</Label>
                <Input
                  className="h-9"
                  placeholder="e.g. Chicago"
                  value={filter.origin}
                  list="loads-origin-city-list"
                  onChange={(e) => {
                    const v = e.target.value;
                    const match = originOptions.find(
                      (o) =>
                        `${o.city}, ${o.stateCode}`.toLowerCase() === v.trim().toLowerCase() ||
                        o.city.toLowerCase() === v.trim().toLowerCase(),
                    );
                    setFilter((p) => ({ ...p, origin: v, originId: match ? String(match.id) : '' }));
                  }}
                  onKeyDown={(e) => e.key === 'Enter' && reload()}
                />
                <datalist id="loads-origin-city-list">
                  {originOptions.map((c) => (
                    <option key={c.id} value={`${c.city}, ${c.stateCode}`} />
                  ))}
                </datalist>
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Destination city</Label>
                <Input
                  className="h-9"
                  placeholder="e.g. Dallas"
                  value={filter.destination}
                  list="loads-destination-city-list"
                  onChange={(e) => {
                    const v = e.target.value;
                    const match = destinationOptions.find(
                      (o) =>
                        `${o.city}, ${o.stateCode}`.toLowerCase() === v.trim().toLowerCase() ||
                        o.city.toLowerCase() === v.trim().toLowerCase(),
                    );
                    setFilter((p) => ({ ...p, destination: v, destinationId: match ? String(match.id) : '' }));
                  }}
                  onKeyDown={(e) => e.key === 'Enter' && reload()}
                />
                <datalist id="loads-destination-city-list">
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
                {loadTypeLoading && <div className="text-[11px] text-muted-foreground">Loading…</div>}
              </div>
              <div className="flex gap-2 items-end">
                <Button type="button" onClick={() => reload()} variant="default" className="h-9 px-5">
                  Search
                </Button>
                {hasAnyFilter && (
                  <Button type="button" onClick={clearFilters} variant="outline" className="h-9 px-5">
                    Clear
                  </Button>
                )}
              </div>
            </div>
            <div className="flex flex-wrap gap-2 justify-end">
              <Button type="button" variant="outline" onClick={manualRefresh} disabled={refreshing}>
                <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
              </Button>
              {canCreateLoads(session?.user as any, session?.company) && (
                <Button
                  onClick={() => {
                    setCreateError('');
                    setOpenCreate(true);
                  }}
                >
                  <Plus className="h-4 w-4" /> New load
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
                  <th>Origin</th>
                  <th>Destination</th>
                  <th>Equipment</th>
                  <th>Pickup</th>
                  <th>Status</th>
                  <th>Carrier</th>
                  <th className="text-right">Revenue</th>
                  <th className="text-right">Profit</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {pag.pageRows.map((l) => {
                  const isOwner =
                    isAdmin(session?.user as any) ||
                    (session?.user.role === 'shipper' &&
                      String(l.shipperUserId) === session?.user?.sub);
                  const refLabel = (l.refId || '').trim() || getId(l).slice(-6);
                  return (
                    <tr key={getId(l)} className="border-b last:border-0 align-top">
                      <td className="py-2 font-medium">{l.refId || getId(l).slice(-6)}</td>
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
                      <td className="text-sm max-w-[180px]">
                        {l.assignedCarrier ? (
                          <div>
                            <div className="font-medium truncate" title={l.assignedCarrier.fullName}>
                              {l.assignedCarrier.fullName}
                            </div>
                            {l.assignedCarrier.companyName && (
                              <div
                                className="text-xs text-muted-foreground truncate"
                                title={l.assignedCarrier.companyName}
                              >
                                {l.assignedCarrier.companyName}
                              </div>
                            )}
                          </div>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </td>
                      <td className="text-right">{fmtMoney(l.billedToCustomer)}</td>
                      <td className="text-right text-emerald-600">{fmtMoney(l.profit)}</td>
                      <td className="text-right">
                        {isOwner && (
                          <div className="flex gap-1 justify-end flex-wrap">
                            <Button size="sm" variant="ghost" title="Edit" onClick={() => openEdit(l)}>
                              <Pencil className="h-3 w-3" />
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              title="Duplicate"
                              onClick={() => duplicateFromRow(l)}
                            >
                              <Copy className="h-3 w-3" />
                            </Button>
                            <Button
                              size="sm"
                              variant="ghost"
                              className="text-rose-700 hover:bg-rose-50"
                              title="Delete"
                              onClick={() => remove(getId(l), refLabel)}
                            >
                              <Trash2 className="h-3 w-3" />
                            </Button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
                {pag.total === 0 && (
                  <tr>
                    <td colSpan={10} className="py-8 text-center text-muted-foreground">
                      No loads found.
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

      <Dialog
        open={openCreate}
        onOpenChange={(open) => {
          setOpenCreate(open);
          if (!open) setCreateError('');
        }}
      >
        <DialogContent className="max-w-[1050px] w-[92vw] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>New load</DialogTitle>
          </DialogHeader>
          {createError && <div className="text-sm text-destructive">{createError}</div>}
          <LoadForm
            requireShipperCompany
            shipperCompanyOptions={shipperCompanyOptions}
            shipperCompanyLoading={shipperCompanyLoading}
            loadTypeOptions={loadTypeOptions}
            loadTypeLoading={loadTypeLoading}
            onSubmit={onCreate}
            onCancel={() => setOpenCreate(false)}
            saving={saving}
            submitLabel="Create"
          />
        </DialogContent>
      </Dialog>

      <Dialog
        open={!!editing}
        onOpenChange={(open) => {
          if (!open) {
            setEditing(null);
            setEditError('');
          }
        }}
      >
        <DialogContent className="max-w-[1050px] w-[92vw] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit load</DialogTitle>
          </DialogHeader>
          {editError && <div className="text-sm text-destructive">{editError}</div>}
          {isAdmin(session?.user as any) && editing && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 rounded-lg border bg-muted/30 p-4">
              <div className="rounded-md border bg-background p-3 space-y-1">
                <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Shipper</div>
                {editing.shipper &&
                (editing.shipper.fullName || editing.shipper.companyName || editing.shipper.email) ? (
                  <>
                    {editing.shipper.fullName && (
                      <div className="text-sm font-medium text-foreground">{editing.shipper.fullName}</div>
                    )}
                    {editing.shipper.companyName && (
                      <div className="text-sm text-muted-foreground">{editing.shipper.companyName}</div>
                    )}
                    {editing.shipper.email && (
                      <div className="text-xs text-muted-foreground">{editing.shipper.email}</div>
                    )}
                  </>
                ) : (
                  <span className="text-sm text-muted-foreground">No shipper on file</span>
                )}
              </div>
              <div
                className={`rounded-md border p-3 space-y-1 ${
                  editing.assignedCarrier
                    ? 'border-emerald-200/60 bg-emerald-50/50 dark:bg-emerald-950/20'
                    : 'border-dashed bg-background'
                }`}
              >
                <div className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Carrier</div>
                {editing.assignedCarrier ? (
                  <>
                    <div className="text-sm font-medium text-foreground">{editing.assignedCarrier.fullName}</div>
                    {editing.assignedCarrier.companyName && (
                      <div className="text-sm text-muted-foreground">{editing.assignedCarrier.companyName}</div>
                    )}
                    {editing.assignedCarrier.email && (
                      <div className="text-xs text-muted-foreground">{editing.assignedCarrier.email}</div>
                    )}
                  </>
                ) : (
                  <span className="text-sm text-muted-foreground">Not assigned yet</span>
                )}
              </div>
            </div>
          )}
          {!isAdmin(session?.user as any) && editing?.assignedCarrier && (
            <div className="rounded-lg border border-emerald-200/60 bg-emerald-50/50 dark:bg-emerald-950/20 p-4 space-y-1">
              <div className="text-sm font-medium text-foreground">Assigned carrier</div>
              <div className="text-sm">{editing.assignedCarrier.fullName}</div>
              {editing.assignedCarrier.companyName && (
                <div className="text-sm text-muted-foreground">{editing.assignedCarrier.companyName}</div>
              )}
              {editing.assignedCarrier.email && (
                <div className="text-xs text-muted-foreground">{editing.assignedCarrier.email}</div>
              )}
              {!workflowForEdit?.canExecCarrierLeg && (
                <p className="text-xs text-muted-foreground pt-2">
                  The assigned carrier (or their dispatcher) moves the load to in transit, then delivered. An
                  administrator marks it completed after delivery.
                </p>
              )}
            </div>
          )}
          {editing && workflowForEdit && (
            <div className="rounded-lg border bg-muted/50 p-4 space-y-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-sm font-medium text-foreground">Workflow status</span>
                <StatusBadge status={editing.status} />
              </div>
              <p className="text-xs text-muted-foreground">
                {editing.assignedCarrier || ['in_transit', 'delivered', 'completed'].includes(
                  String(editing.status || '').toLowerCase(),
                ) ? (
                  <span className="block">
                    <strong>Carriers</strong> (or their dispatchers) set <strong>In transit</strong> then{' '}
                    <strong>Delivered</strong> on assigned loads—also in <strong>Carrier portal</strong>.{' '}
                    <strong>Administrators</strong> set <strong>Completed</strong> only after the load is delivered.
                  </span>
                ) : (
                  'Move the load through your process here. Posted loads are visible in the carrier portal.'
                )}
              </p>
              <div className="flex flex-wrap gap-2">
                {workflowForEdit.canPost && (
                  <Button
                    type="button"
                    size="sm"
                    variant="secondary"
                    disabled={statusBusy}
                    onClick={() => setLoadStatusInEditor('posted')}
                  >
                    Post to board
                  </Button>
                )}
                {workflowForEdit.canExecCarrierLeg &&
                  ['assigned', 'claimed'].includes(String(editing.status || '').toLowerCase()) && (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={statusBusy}
                      onClick={() => setLoadStatusInEditor('in_transit')}
                    >
                      In transit
                    </Button>
                  )}
                {workflowForEdit.canExecCarrierLeg &&
                  String(editing.status || '').toLowerCase() === 'in_transit' && (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={statusBusy}
                      onClick={() => setLoadStatusInEditor('delivered')}
                    >
                      Delivered
                    </Button>
                  )}
                {workflowForEdit.canMarkCompleted &&
                  String(editing.status || '').toLowerCase() === 'delivered' && (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={statusBusy}
                      onClick={() => setLoadStatusInEditor('completed')}
                    >
                      Completed
                    </Button>
                  )}
                {workflowForEdit.canCancel && (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="text-rose-700 border-rose-200 hover:bg-rose-50"
                    disabled={statusBusy}
                    onClick={() => setLoadStatusInEditor('cancelled')}
                  >
                    Cancel load
                  </Button>
                )}
                {!workflowForEdit.canPost &&
                  !workflowForEdit.canExecCarrierLeg &&
                  !workflowForEdit.canMarkCompleted &&
                  !workflowForEdit.canCancel && (
                    <span className="text-xs text-muted-foreground">No status actions for your role or this state.</span>
                  )}
              </div>
            </div>
          )}
          {editing && (
            <LoadForm
              key={getId(editing)}
              initial={editing}
              requireShipperCompany
              shipperCompanyOptions={shipperCompanyOptions}
              shipperCompanyLoading={shipperCompanyLoading}
              loadTypeOptions={loadTypeOptions}
              loadTypeLoading={loadTypeLoading}
              onSubmit={onUpdate}
              onCancel={() => setEditing(null)}
              saving={saving}
              submitLabel="Save changes"
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
