'use client';
import { useEffect, useMemo, useState } from 'react';
import { Api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { fmtMoney, fmtPercent } from '@/lib/utils';
import { useUser } from '@/lib/user-context';
import { canSetCarrierPay } from '@/lib/permissions';
import { ProfitDonut } from '@/components/profit-donut';
import { PlacesFieldset } from '@/components/places-fieldset';

export type LoadFormValues = {
  /** Required when creating a load (validated server-side). */
  shipperCompanyId?: number;
  refId?: string;
  equipmentType?: string;
  trailerLengthFt?: number;
  weightLbs?: number;
  commodity?: string;
  pickUpDate?: string;
  deliveryDate?: string;
  loadDate?: string;
  untilDate?: string;
  isLoadFull?: boolean;
  allowUntilSat?: boolean;
  allowUntilSun?: boolean;
  billedToCustomer?: number;
  payToCarrier?: number;
  /** @deprecated Prefer description + userNotes; still accepted from older API responses. */
  notes?: string;
  description?: string;
  userNotes?: string;
  loadTypeId?: number;
  origin?: { city?: string; state?: string; zip?: string };
  destination?: { city?: string; state?: string; zip?: string };
};

function normState(s?: string) {
  return (s || '').trim().toUpperCase();
}

function toDateTimeLocal(v?: string) {
  if (!v) return '';
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return '';
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  const hh = String(d.getHours()).padStart(2, '0');
  const mi = String(d.getMinutes()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}T${hh}:${mi}`;
}

function fromDateTimeLocal(v?: string) {
  if (!v) return '';
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return '';
  return d.toISOString();
}

function toDateOnlyLocal(v?: string) {
  if (!v) return '';
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) return '';
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function fromDateOnlyLocal(v?: string) {
  if (!v) return '';
  return new Date(`${v}T00:00:00`).toISOString();
}

function mergeInitial(initial?: LoadFormValues): LoadFormValues {
  if (!initial) {
    return {
      equipmentType: 'Dry Van',
      trailerLengthFt: 53,
      loadDate: new Date().toISOString(),
      isLoadFull: false,
      allowUntilSat: false,
      allowUntilSun: false,
      description: '',
      userNotes: '',
      origin: { city: '', state: '', zip: '' },
      destination: { city: '', state: '', zip: '' },
    };
  }
  const { notes: legacyNotes, ...restInitial } = initial;
  const anyInitial = initial as any;
  const shipperCo =
    typeof anyInitial?.shipperCompanyId === 'number'
      ? anyInitial.shipperCompanyId
      : typeof anyInitial?.shipper?.companyId === 'number'
        ? anyInitial.shipper.companyId
        : undefined;
  return {
    ...restInitial,
    shipperCompanyId: shipperCo,
    equipmentType: initial.equipmentType || 'Dry Van',
    trailerLengthFt: initial.trailerLengthFt ?? 53,
    loadDate: initial.loadDate || new Date().toISOString(),
    untilDate: initial.untilDate || '',
    isLoadFull: !!initial.isLoadFull,
    allowUntilSat: !!initial.allowUntilSat,
    allowUntilSun: !!initial.allowUntilSun,
    description:
      typeof initial.description === 'string' ? initial.description : (legacyNotes || ''),
    userNotes: typeof initial.userNotes === 'string' ? initial.userNotes : '',
    origin: {
      city: initial.origin?.city || '',
      state: normState(initial.origin?.state),
      zip: initial.origin?.zip || '',
    },
    destination: {
      city: initial.destination?.city || '',
      state: normState(initial.destination?.state),
      zip: initial.destination?.zip || '',
    },
  };
}

export function LoadForm({
  initial,
  onSubmit,
  onCancel,
  saving,
  submitLabel = 'Save',
  shipperCompanyOptions = [],
  requireShipperCompany = false,
  shipperCompanyLoading = false,
  loadTypeOptions = [],
  loadTypeLoading = false,
}: {
  initial?: LoadFormValues;
  onSubmit: (v: LoadFormValues) => void;
  onCancel?: () => void;
  saving?: boolean;
  submitLabel?: string;
  /** When creating a load, list of shipper companies (admin: all; shipper: usually one). */
  shipperCompanyOptions?: Array<{ id: number; name: string }>;
  requireShipperCompany?: boolean;
  shipperCompanyLoading?: boolean;
  loadTypeOptions?: Array<{ id: number; name: string }>;
  loadTypeLoading?: boolean;
}) {
  const session = useUser();
  const staffCanSetCarrierPay = canSetCarrierPay(session?.user as any);
  const [form, setForm] = useState<LoadFormValues>(() => mergeInitial(initial));
  const [templates, setTemplates] = useState<any[]>([]);
  const [tplId, setTplId] = useState<string>('');
  const [validationError, setValidationError] = useState('');
  const [localLoadTypes, setLocalLoadTypes] = useState<Array<{ id: number; name: string }>>([]);
  const [localLoadTypesLoading, setLocalLoadTypesLoading] = useState(false);
  const activeLoadTypeOptions = loadTypeOptions.length > 0 ? loadTypeOptions : localLoadTypes;
  const activeLoadTypeLoading = loadTypeLoading || (loadTypeOptions.length === 0 && localLoadTypesLoading);
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const initialKey = initial ? String((initial as any).id ?? (initial as any)._id ?? '') : '';

  useEffect(() => {
    Api.templates().then(setTemplates).catch(() => {});
  }, []);

  useEffect(() => {
    if (loadTypeOptions.length > 0) return;
    setLocalLoadTypesLoading(true);
    Api.loadTypes()
      .then(setLocalLoadTypes)
      .catch(() => setLocalLoadTypes([]))
      .finally(() => setLocalLoadTypesLoading(false));
  }, [loadTypeOptions.length]);

  useEffect(() => {
    setForm(mergeInitial(initial));
    setTplId('');
  }, [initialKey]);

  useEffect(() => {
    if (!requireShipperCompany || shipperCompanyOptions.length !== 1) return;
    setForm((p) => (p.shipperCompanyId != null ? p : { ...p, shipperCompanyId: shipperCompanyOptions[0].id }));
  }, [requireShipperCompany, shipperCompanyOptions]);

  useEffect(() => {
    if (activeLoadTypeOptions.length === 0) return;
    const hasId = Number(form.loadTypeId || 0) > 0;
    if (hasId) return;
    const byName = activeLoadTypeOptions.find(
      (x) => (x.name || '').trim().toLowerCase() === String(form.equipmentType || '').trim().toLowerCase(),
    );
    const pick = byName || activeLoadTypeOptions[0];
    setForm((p) => ({ ...p, loadTypeId: pick.id, equipmentType: pick.name }));
  }, [activeLoadTypeOptions, form.loadTypeId, form.equipmentType]);

  function update<K extends keyof LoadFormValues>(k: K, v: LoadFormValues[K]) {
    setForm((p) => ({ ...p, [k]: v }));
  }

  function applyTemplate(id: string) {
    setTplId(id);
    if (!id) return;
    const t = templates.find((x) => getId(x) === id);
    if (!t) return;
    // Legacy MVC: one template `Notes` string is written to both Description and UserNotes.
    const tplText = String((t as { notes?: string }).notes ?? (t as { Notes?: string }).Notes ?? '');
    setForm((p) => ({
      ...p,
      equipmentType: t.equipmentType || p.equipmentType,
      loadTypeId: t.loadTypeId ?? p.loadTypeId,
      trailerLengthFt: t.trailerLengthFt ?? p.trailerLengthFt,
      weightLbs: t.weightLbs ?? p.weightLbs,
      origin: {
        city: t.origin?.city ?? p.origin?.city ?? '',
        state: normState(t.origin?.state ?? p.origin?.state),
        zip: t.origin?.zip ?? p.origin?.zip ?? '',
      },
      destination: {
        city: t.destination?.city ?? p.destination?.city ?? '',
        state: normState(t.destination?.state ?? p.destination?.state),
        zip: t.destination?.zip ?? p.destination?.zip ?? '',
      },
      description: tplText,
      userNotes: tplText,
    }));
  }

  const profit = useMemo(() => {
    const billed = Number(form.billedToCustomer || 0);
    const cost = Number(form.payToCarrier || 0);
    return { billed, cost, profit: billed - cost, margin: billed > 0 ? ((billed - cost) / billed) * 100 : 0 };
  }, [form.billedToCustomer, form.payToCarrier]);

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        setValidationError('');
        if (!form.loadTypeId) {
          setValidationError('Equipment type is required.');
          return;
        }
        if (!form.pickUpDate) {
          setValidationError('Pickup date is required.');
          return;
        }
        if (!form.deliveryDate) {
          setValidationError('Delivery date is required.');
          return;
        }
        if (!String(form.destination?.city || '').trim() || !String(form.destination?.state || '').trim()) {
          setValidationError('Destination city + state are required.');
          return;
        }
        onSubmit(form);
      }}
      className="grid grid-cols-1 lg:grid-cols-3 gap-5"
    >
      <div className="lg:col-span-2 space-y-4">
        {requireShipperCompany && (
          <div className="space-y-2">
            <Label>Company Name *</Label>
            {shipperCompanyLoading ? (
              <p className="text-sm text-muted-foreground">Loading shipper companies…</p>
            ) : shipperCompanyOptions.length === 0 ? (
              <p className="text-sm text-destructive">
                No shipper companies are available. Your account may need to be linked to a shipper company, or an admin
                must add shipper companies first.
              </p>
            ) : (
              <Select
                value={form.shipperCompanyId != null ? String(form.shipperCompanyId) : ''}
                onChange={(e) =>
                  update('shipperCompanyId', e.target.value ? Number(e.target.value) : undefined)
                }
                required
              >
                <option value="">Select company…</option>
                {shipperCompanyOptions.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name}
                  </option>
                ))}
              </Select>
            )}
          </div>
        )}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Reference / PRO #</Label>
            <Input value={form.refId || ''} onChange={(e) => update('refId', e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>Apply template</Label>
            <Select value={tplId} onChange={(e) => applyTemplate(e.target.value)}>
              <option value="">— Select template —</option>
              {templates.map((t) => (
                <option key={getId(t)} value={getId(t)}>
                  {t.name}
                  {t.isGlobal ? ' (Global)' : ''}
                </option>
              ))}
            </Select>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="space-y-2">
            <Label>Equipment type</Label>
            {activeLoadTypeLoading ? (
              <p className="text-sm text-muted-foreground">Loading equipment types…</p>
            ) : (
              <Select
                value={form.loadTypeId != null ? String(form.loadTypeId) : ''}
                onChange={(e) => {
                  const id = e.target.value ? Number(e.target.value) : undefined;
                  const sel = activeLoadTypeOptions.find((x) => x.id === id);
                  update('loadTypeId', id);
                  if (sel) update('equipmentType', sel.name);
                }}
                required
              >
                <option value="">Select equipment type…</option>
                {activeLoadTypeOptions.map((lt) => (
                  <option key={lt.id} value={String(lt.id)}>
                    {lt.name}
                  </option>
                ))}
              </Select>
            )}
          </div>
          <div className="space-y-2">
            <Label>Length (ft)</Label>
            <Input
              type="number"
              value={form.trailerLengthFt ?? ''}
              onChange={(e) => update('trailerLengthFt', e.target.value ? Number(e.target.value) : undefined)}
            />
          </div>
          <div className="space-y-2">
            <Label>Weight (lbs)</Label>
            <Input
              type="number"
              value={form.weightLbs ?? ''}
              onChange={(e) => update('weightLbs', e.target.value ? Number(e.target.value) : undefined)}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Commodity</Label>
            <Input value={form.commodity || ''} onChange={(e) => update('commodity', e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>Load date *</Label>
            <Input
              type="date"
              value={toDateOnlyLocal(form.loadDate)}
              onChange={(e) => update('loadDate', fromDateOnlyLocal(e.target.value))}
              required
            />
          </div>
          <div className="space-y-2">
            <Label>Pickup date *</Label>
            <div className="grid grid-cols-1 sm:grid-cols-[1fr_130px] gap-2">
              <Input
                type="date"
                value={toDateOnlyLocal(form.pickUpDate)}
                onChange={(e) => {
                  const nextDate = e.target.value;
                  const existing = toDateTimeLocal(form.pickUpDate);
                  const timePart = existing && existing.includes('T') ? existing.split('T')[1] : '00:00';
                  update('pickUpDate', fromDateTimeLocal(nextDate ? `${nextDate}T${timePart}` : ''));
                }}
                required
              />
              <Input
                type="time"
                value={(toDateTimeLocal(form.pickUpDate).split('T')[1] || '00:00').slice(0, 5)}
                onChange={(e) => {
                  const datePart = toDateOnlyLocal(form.pickUpDate);
                  update('pickUpDate', fromDateTimeLocal(datePart ? `${datePart}T${e.target.value}` : ''));
                }}
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label>Delivery date *</Label>
            <div className="grid grid-cols-1 sm:grid-cols-[1fr_130px] gap-2">
              <Input
                type="date"
                value={toDateOnlyLocal(form.deliveryDate)}
                onChange={(e) => {
                  const nextDate = e.target.value;
                  const existing = toDateTimeLocal(form.deliveryDate);
                  const timePart = existing && existing.includes('T') ? existing.split('T')[1] : '00:00';
                  update('deliveryDate', fromDateTimeLocal(nextDate ? `${nextDate}T${timePart}` : ''));
                }}
                required
              />
              <Input
                type="time"
                value={(toDateTimeLocal(form.deliveryDate).split('T')[1] || '00:00').slice(0, 5)}
                onChange={(e) => {
                  const datePart = toDateOnlyLocal(form.deliveryDate);
                  update('deliveryDate', fromDateTimeLocal(datePart ? `${datePart}T${e.target.value}` : ''));
                }}
              />
            </div>
          </div>
        </div>

        <div className="rounded-md border p-3">
          <div className="text-sm font-medium mb-2">Operational</div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 items-end">
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={!!form.isLoadFull}
                onChange={(e) => update('isLoadFull', e.target.checked)}
              />
              Is load full
            </label>
            <div className="space-y-2">
              <Label>Until date</Label>
              <Input
                type="date"
                value={toDateOnlyLocal(form.untilDate)}
                onChange={(e) => update('untilDate', fromDateOnlyLocal(e.target.value))}
              />
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={!!form.allowUntilSat}
                onChange={(e) => update('allowUntilSat', e.target.checked)}
              />
              Roll over to Saturday
            </label>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={!!form.allowUntilSun}
                onChange={(e) => update('allowUntilSun', e.target.checked)}
              />
              Roll over to Sunday
            </label>
          </div>
        </div>

        <PlacesFieldset
          origin={{
            city: form.origin?.city || '',
            state: form.origin?.state || '',
          }}
          destination={{
            city: form.destination?.city || '',
            state: form.destination?.state || '',
          }}
          onOrigin={(patch) =>
            setForm((p) => ({
              ...p,
              origin: { city: '', state: '', zip: '', ...(p.origin || {}), ...patch },
            }))
          }
          onDestination={(patch) =>
            setForm((p) => ({
              ...p,
              destination: { city: '', state: '', zip: '', ...(p.destination || {}), ...patch },
            }))
          }
        />

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Description</Label>
            <Textarea value={form.description || ''} onChange={(e) => update('description', e.target.value)} rows={4} />
          </div>
          <div className="space-y-2">
            <Label>User notes</Label>
            <Textarea value={form.userNotes || ''} onChange={(e) => update('userNotes', e.target.value)} rows={4} />
          </div>
        </div>

        <div className="flex items-center justify-end gap-2 pt-2">
          {validationError && <div className="mr-auto text-sm text-destructive">{validationError}</div>}
          {onCancel && (
            <Button variant="outline" type="button" onClick={onCancel}>
              Cancel
            </Button>
          )}
          <Button type="submit" disabled={saving}>
            {saving ? 'Saving…' : submitLabel}
          </Button>
        </div>
      </div>

      <div className="space-y-4">
        <div className="rounded-lg border p-4 bg-muted/30">
          <div className="text-sm font-medium mb-2">Rate</div>
          <div className="space-y-3">
            <div className="space-y-2">
              <Label>Billed to customer (USD)</Label>
              <Input
                type="number"
                value={form.billedToCustomer ?? ''}
                onChange={(e) => update('billedToCustomer', e.target.value ? Number(e.target.value) : 0)}
              />
            </div>
            <div className="space-y-2">
              <Label>Pay to carrier (USD)</Label>
              <Input
                type="number"
                value={form.payToCarrier ?? ''}
                disabled={!staffCanSetCarrierPay}
                onChange={(e) => update('payToCarrier', e.target.value ? Number(e.target.value) : 0)}
              />
              {!staffCanSetCarrierPay && (
                <div className="text-xs text-muted-foreground">Only administrators set carrier pay.</div>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div className="rounded border p-2">
                <div className="text-muted-foreground text-xs">Profit</div>
                <div className="font-semibold text-emerald-600">{fmtMoney(profit.profit)}</div>
              </div>
              <div className="rounded border p-2">
                <div className="text-muted-foreground text-xs">Margin</div>
                <div className="font-semibold">{fmtPercent(profit.margin)}</div>
              </div>
            </div>
          </div>
        </div>

        <div className="rounded-lg border p-4">
          <ProfitDonut revenue={profit.billed} cost={profit.cost} />
        </div>
      </div>
    </form>
  );
}
