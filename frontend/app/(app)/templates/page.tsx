'use client';
import { useEffect, useState } from 'react';
import { Api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { useUser } from '@/lib/user-context';
import { canCreateLoads, isAdmin } from '@/lib/permissions';
import { Trash2 } from 'lucide-react';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';
import { confirmDelete } from '@/lib/confirm-action';
import { PlacesFieldset } from '@/components/places-fieldset';

export default function TemplatesPage() {
  const session = useUser();
  const getId = (x: any) => String(x?.id ?? x?._id ?? '');
  const staff = isAdmin(session?.user as any);
  const allowed = canCreateLoads(session?.user as any, session?.company);
  const [list, setList] = useState<any[]>([]);
  const [companies, setCompanies] = useState<any[]>([]);
  const [loadTypes, setLoadTypes] = useState<Array<{ id: number; name: string }>>([]);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<any>({
    name: '',
    isGlobal: false,
    companyId: '',
    equipmentType: '',
    loadTypeId: '',
    trailerLengthFt: 53,
    weightLbs: '',
    origin: { city: '', state: '' },
    destination: { city: '', state: '' },
    description: '',
    userNotes: '',
  });
  const pag = useClientPagination(list, []);

  async function reload() {
    const tpls = await Api.templates();
    setList(tpls);
    const lts = await Api.loadTypes();
    setLoadTypes(lts);
    if (staff) {
      const cos = await Api.companies({ type: 'shipper', status: 'approved' });
      setCompanies(cos);
    }
  }
  useEffect(() => {
    if (!allowed) return;
    reload().catch(() => {});
  }, [staff, allowed]);

  if (!allowed) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Templates</CardTitle>
        </CardHeader>
        <CardContent className="py-8 text-center text-sm text-muted-foreground">
          Templates are for staff, shippers, and dispatchers who create loads. Carrier accounts do not use this page.
        </CardContent>
      </Card>
    );
  }

  function update<K extends keyof typeof form>(k: K, v: any) {
    setForm((p: any) => ({ ...p, [k]: v }));
  }
  async function save() {
    setError('');
    setSaving(true);
    try {
      const payload: any = { ...form };
      // Legacy LoadTemplates.Notes = UserNotes || Description (trimmed; user notes wins).
      const u = typeof form.userNotes === 'string' ? form.userNotes.trim() : '';
      const d = typeof form.description === 'string' ? form.description.trim() : '';
      payload.notes = u || d || '';
      delete payload.description;
      delete payload.userNotes;
      if (form.weightLbs === '') payload.weightLbs = undefined;
      const selectedType = loadTypes.find((lt) => String(lt.id) === String(form.loadTypeId || ''));
      if (!selectedType) throw new Error('Select an equipment type');
      if (selectedType) {
        payload.loadTypeId = selectedType.id;
        payload.equipmentType = selectedType.name;
      }
      if (!staff) {
        payload.isGlobal = false;
        delete payload.companyId;
      } else if (!payload.isGlobal && !payload.companyId) {
        throw new Error('Select a shipper company for non-global template');
      }
      await Api.saveTemplate(payload);
      await reload();
      setForm({
        name: '',
        isGlobal: false,
        companyId: '',
        equipmentType: '',
        loadTypeId: '',
        trailerLengthFt: 53,
        weightLbs: '',
        origin: { city: '', state: '' },
        destination: { city: '', state: '' },
        description: '',
        userNotes: '',
      });
    } catch (err: any) {
      setError(err.message || 'Save failed');
    } finally {
      setSaving(false);
    }
  }

  async function remove(id: string, name: string) {
    if (!confirmDelete({ subject: 'this template', name })) return;
    await Api.deleteTemplate(id);
    await reload();
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <Card className="lg:col-span-2">
        <CardHeader>
          <CardTitle>Templates</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="text-left text-muted-foreground border-b">
                <tr>
                  <th className="py-2">Name</th>
                  <th>Scope</th>
                  <th>Equipment</th>
                  <th>Origin</th>
                  <th>Destination</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {pag.pageRows.map((t) => (
                  <tr key={getId(t)} className="border-b last:border-0">
                    <td className="py-2 font-medium">{t.name}</td>
                    <td>
                      {t.isGlobal ? (
                        <span className="text-blue-700 text-xs bg-blue-50 rounded px-2 py-0.5">Global</span>
                      ) : (
                        <span className="text-slate-600 text-xs bg-slate-100 rounded px-2 py-0.5">Company</span>
                      )}
                    </td>
                    <td>
                      {loadTypes.find((lt) => lt.id === Number(t.loadTypeId))?.name || t.equipmentType}{' '}
                      {t.trailerLengthFt ? `· ${t.trailerLengthFt}ft` : ''}
                    </td>
                    <td>
                      {t.origin?.city}, {t.origin?.state}
                    </td>
                    <td>
                      {t.destination?.city}, {t.destination?.state}
                    </td>
                    <td className="text-right">
                      <Button size="sm" variant="ghost" onClick={() => remove(getId(t), t.name || 'Template')}>
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </td>
                  </tr>
                ))}
                {pag.total === 0 && (
                  <tr>
                    <td colSpan={6} className="py-6 text-center text-muted-foreground">
                      No templates yet.
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

      <Card>
        <CardHeader>
          <CardTitle>Create template</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="space-y-2">
            <Label>Name</Label>
            <Input value={form.name} onChange={(e) => update('name', e.target.value)} />
          </div>
          {staff && (
            <>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.isGlobal}
                  onChange={(e) => update('isGlobal', e.target.checked)}
                />
                Global template (visible to all shippers)
              </label>
              {!form.isGlobal && (
                <div className="space-y-2">
                  <Label>Shipper company</Label>
                  <Select value={form.companyId} onChange={(e) => update('companyId', e.target.value)}>
                    <option value="">— Select company —</option>
                    {companies.map((c) => (
                      <option key={getId(c)} value={getId(c)}>
                        {c.name}
                      </option>
                    ))}
                  </Select>
                </div>
              )}
            </>
          )}
          <div className="grid grid-cols-2 gap-2">
            <div className="space-y-2">
              <Label>Equipment</Label>
              <Select
                value={form.loadTypeId}
                onChange={(e) => {
                  const v = e.target.value;
                  const lt = loadTypes.find((x) => String(x.id) === v);
                  update('loadTypeId', v);
                  update('equipmentType', lt?.name || '');
                }}
              >
                <option value="">— Select equipment type —</option>
                {loadTypes.map((lt) => (
                  <option key={lt.id} value={String(lt.id)}>
                    {lt.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Length (ft)</Label>
              <Input
                type="number"
                value={form.trailerLengthFt}
                onChange={(e) => update('trailerLengthFt', e.target.value ? Number(e.target.value) : '')}
              />
            </div>
          </div>
          <div className="space-y-2">
            <Label>Weight (lbs)</Label>
            <Input
              type="number"
              value={form.weightLbs}
              onChange={(e) => update('weightLbs', e.target.value ? Number(e.target.value) : '')}
            />
          </div>
          <PlacesFieldset
            origin={{ city: form.origin.city, state: form.origin.state }}
            destination={{ city: form.destination.city, state: form.destination.state }}
            onOrigin={(patch) => setForm((p: any) => ({ ...p, origin: { ...p.origin, ...patch } }))}
            onDestination={(patch) =>
              setForm((p: any) => ({ ...p, destination: { ...p.destination, ...patch } }))
            }
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Description</Label>
              <Textarea
                value={form.description}
                onChange={(e) => update('description', e.target.value)}
                rows={4}
              />
            </div>
            <div className="space-y-2">
              <Label>User notes</Label>
              <Textarea
                value={form.userNotes}
                onChange={(e) => update('userNotes', e.target.value)}
                rows={4}
              />
            </div>
          </div>
          <p className="text-xs text-muted-foreground">
            Stored as one template note: user notes override description when both are set (same as legacy load board).
          </p>
          {error && <div className="text-sm text-destructive">{error}</div>}
          <Button className="w-full" disabled={saving || !form.name} onClick={save}>
            {saving ? 'Saving…' : 'Save template'}
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
