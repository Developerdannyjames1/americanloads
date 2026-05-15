'use client';

import { useEffect, useMemo, useState } from 'react';

import { Api } from '@/lib/api';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

import { Button } from '@/components/ui/button';

import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';

import { Input } from '@/components/ui/input';

import { Label } from '@/components/ui/label';

import { Select } from '@/components/ui/select';

import { fmtDate } from '@/lib/utils';
import { evaluatePasswordPolicy, isPasswordPolicyValid } from '@/lib/password-policy';
import { useUser } from '@/lib/user-context';
import { TablePagination } from '@/components/table-pagination';
import { useClientPagination } from '@/lib/use-client-pagination';
import { confirmDelete, confirmPrompt } from '@/lib/confirm-action';
import { Check, Pencil, Trash2, UserPlus, UserX } from 'lucide-react';

type UserModal = { mode: 'add' } | { mode: 'edit'; user: any };



const ROLE_OPTIONS = [

  { value: 'admin', label: 'Admin' },

  { value: 'shipper', label: 'Shipper' },

  { value: 'carrier', label: 'Carrier' },

  { value: 'dispatcher', label: 'Dispatcher' },

];



const CARRIER_STATUS = ['pending', 'approved', 'rejected', 'suspended', 'needs_review'];



export default function UsersPage() {
  const session = useUser();
  const adminOnly = session?.user.role === 'admin';

  const [rows, setRows] = useState<any[]>([]);

  const [companies, setCompanies] = useState<any[]>([]);
  const [locations, setLocations] = useState<Array<{ id?: number; code: string; name: string }>>([]);

  const getId = (x: any) => String(x?.id ?? x?._id ?? '');

  const [modal, setModal] = useState<UserModal | null>(null);

  const [form, setForm] = useState({

    username: '',

    email: '',

    fullName: '',
    location: '',
    extension: '',

    role: 'shipper',

    companySel: '__keep__',

    carrierStatusSel: '__keep__',

    password: '',
    confirmPassword: '',

  });

  const [busy, setBusy] = useState(false);

  const [msg, setMsg] = useState('');
  const [filters, setFilters] = useState({ q: '', role: '', active: '' });
  const filteredRows = useMemo(() => {
    const q = filters.q.trim().toLowerCase();
    return rows.filter((u) => {
      const roleOk = !filters.role || String(u.role || '').toLowerCase() === filters.role;
      const activeOk =
        !filters.active ||
        (filters.active === 'active' ? !!u.isActive : filters.active === 'inactive' ? !u.isActive : true);
      const textOk =
        !q ||
        [
          u.fullName,
          u.username,
          u.email,
          u.role,
          u.location,
          u.extension,
          u.companyName,
          u.carrierApprovalStatus,
        ]
          .map((v) => String(v || '').toLowerCase())
          .some((v) => v.includes(q));
      return roleOk && activeOk && textOk;
    });
  }, [rows, filters.q, filters.role, filters.active]);
  const pag = useClientPagination(filteredRows, [filters.q, filters.role, filters.active, filteredRows.length]);

  async function reload() {

    const list = await Api.users();

    setRows(list);

  }



  useEffect(() => {

    reload().catch(() => {});

    Api.companies()

      .then(setCompanies)

      .catch(() => setCompanies([]));

    Api.registerLocations()
      .then(setLocations)
      .catch(() => setLocations([]));

  }, []);



  function openEdit(u: any) {
    setModal({ mode: 'edit', user: u });
    setMsg('');
    setForm({
      username: u.username || '',
      email: u.email || '',
      fullName: u.fullName || '',
      location: u.location || '',
      extension: u.extension || '',
      role: (u.role || 'shipper').toLowerCase(),
      companySel: u.companyId != null ? String(u.companyId) : '__none__',
      carrierStatusSel:
        u.carrierApprovalStatus != null && u.carrierApprovalStatus !== ''
          ? u.carrierApprovalStatus
          : '__keep__',
      password: '',
      confirmPassword: '',
    });
  }

  function openAddUser() {
    setModal({ mode: 'add' });
    setMsg('');
    setForm({
      username: '',
      email: '',
      fullName: '',
      location: '',
      extension: '',
      role: 'shipper',
      companySel: '__none__',
      carrierStatusSel: 'pending',
      password: '',
      confirmPassword: '',
    });
  }

  async function saveModal() {
    if (!modal) return;
    setBusy(true);
    setMsg('');
    try {
      if (!form.location.trim()) {
        setMsg('Location is required.');
        return;
      }

      if (modal.mode === 'add') {
        if (!form.username.trim()) {
          setMsg('Username is required.');
          return;
        }
        const pw = form.password.trim();
        if (!pw) {
          setMsg('Password is required for new users.');
          return;
        }
        if (pw !== form.confirmPassword.trim()) {
          setMsg('Password and confirm password do not match.');
          return;
        }
        if (!isPasswordPolicyValid(pw)) {
          setMsg('Password does not meet required rules.');
          return;
        }
        const payload: Record<string, unknown> = {
          username: form.username.trim(),
          email: form.email.trim(),
          password: pw,
          fullName: form.fullName.trim(),
          location: form.location.trim(),
          extension: form.extension.trim() || null,
          role: form.role,
          companyId: form.companySel === '__none__' ? null : Number(form.companySel),
        };
        if (showCarrierApprovalField) {
          payload.carrierApprovalStatus =
            form.carrierStatusSel === '__none__' ? null : form.carrierStatusSel;
        }
        await Api.createUser(payload);
        setModal(null);
        await reload();
        return;
      }

      const payload: Record<string, unknown> = {
        email: form.email.trim(),
        fullName: form.fullName.trim(),
        location: form.location.trim(),
        extension: form.extension.trim() || null,
        role: form.role,
      };
      if (form.companySel === '__none__') payload.companyId = null;
      else if (form.companySel && form.companySel !== '__keep__') {
        payload.companyId = Number(form.companySel);
      }
      if (form.carrierStatusSel === '__none__') payload.carrierApprovalStatus = null;
      else if (form.carrierStatusSel !== '__keep__') {
        payload.carrierApprovalStatus = form.carrierStatusSel;
      }
      const pw = form.password.trim();
      if (pw.length > 0) {
        if (pw !== form.confirmPassword.trim()) {
          setMsg('New password and confirm password do not match.');
          return;
        }
        if (!isPasswordPolicyValid(pw)) {
          setMsg('Password does not meet required rules.');
          return;
        }
        payload.password = pw;
      }
      await Api.updateUser(getId(modal.user), payload);
      setModal(null);
      await reload();
    } catch (e: any) {
      setMsg(e?.message || 'Save failed');
    } finally {
      setBusy(false);
    }
  }



  async function removeUser(u: any) {

    if (!confirmDelete({ subject: 'user', name: u.username || u.email || u.fullName })) return;

    setBusy(true);

    try {

      await Api.deleteUser(getId(u));

      await reload();

    } catch (e: any) {

      alert(e?.message || 'Delete failed');

    } finally {

      setBusy(false);

    }

  }



  async function toggleActive(u: any) {
    if (
      u.isActive &&
      !confirmPrompt(
        `Deactivate ${u.username || u.email}?\n\nThey will not be able to sign in until reactivated.`,
      )
    )
      return;

    setBusy(true);

    try {

      await Api.setUserActive(getId(u), !u.isActive);

      await reload();

    } catch (e: any) {

      alert(e?.message || 'Could not update account status');

    } finally {

      setBusy(false);

    }

  }



  function updateForm<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {

    setForm((p) => ({ ...p, [k]: v }));

  }

  const selectedCompanyType = useMemo(() => {
    if (form.companySel === '__none__' || form.companySel === '__keep__') return '';
    const c = companies.find((x) => String(x.id) === String(form.companySel));
    return String(c?.companyType || '').toLowerCase();
  }, [companies, form.companySel]);

  const showCarrierApprovalField =
    form.role === 'carrier' || (form.role === 'dispatcher' && selectedCompanyType === 'carrier');

  const pwChecks = evaluatePasswordPolicy(form.password);



  return (

    <Card>

      <CardHeader className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between sm:space-y-0">
        <div className="space-y-1">
          <CardTitle>Users</CardTitle>
          <p className="text-sm text-muted-foreground pt-1">
            {adminOnly ? (
              <>
                Add or edit accounts, roles, carrier approval, reset passwords, deactivate, or delete users who are
                not referenced by loads or claims.
              </>
            ) : (
              <>You can browse users; only administrators may change accounts.</>
            )}
          </p>
        </div>
        {adminOnly && (
          <Button type="button" className="shrink-0" onClick={() => openAddUser()}>
            <UserPlus className="h-4 w-4 mr-2" />
            Add user
          </Button>
        )}
      </CardHeader>

      <CardContent>
        <div className="mb-4 grid grid-cols-1 md:grid-cols-4 gap-2 items-end">
          <div className="space-y-1 md:col-span-2">
            <Label className="text-xs">Search</Label>
            <Input
              placeholder="Name, username, email, role, location..."
              value={filters.q}
              onChange={(e) => setFilters((p) => ({ ...p, q: e.target.value }))}
            />
          </div>
          <div className="space-y-1">
            <Label className="text-xs">Role</Label>
            <Select value={filters.role} onChange={(e) => setFilters((p) => ({ ...p, role: e.target.value }))}>
              <option value="">All</option>
              {ROLE_OPTIONS.map((r) => (
                <option key={r.value} value={r.value}>
                  {r.label}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1">
            <Label className="text-xs">Status</Label>
            <Select value={filters.active} onChange={(e) => setFilters((p) => ({ ...p, active: e.target.value }))}>
              <option value="">All</option>
              <option value="active">Active</option>
              <option value="inactive">Inactive</option>
            </Select>
          </div>
        </div>

        <div className="overflow-x-auto">

          <table className="w-full text-sm">

            <thead className="text-left text-muted-foreground border-b">

              <tr>

                <th className="py-2">Name</th>

                <th>Username</th>

                <th>Email</th>

                <th>Role</th>

                <th>Active</th>

                <th>Created</th>

                {adminOnly && <th className="text-right">Actions</th>}

              </tr>

            </thead>

            <tbody>

              {pag.pageRows.map((u) => (

                <tr key={getId(u)} className="border-b last:border-0">

                  <td className="py-2 font-medium">{u.fullName}</td>

                  <td className="font-mono text-xs">{u.username || '—'}</td>

                  <td>{u.email}</td>

                  <td className="capitalize">{u.role}</td>

                  <td>{u.isActive ? 'Yes' : 'No'}</td>

                  <td>{fmtDate(u.createdAt)}</td>

                  {adminOnly && (
                    <td className="text-right">
                      <div className="flex gap-1 justify-end flex-wrap">
                        <Button
                          type="button"
                          size="icon"
                          variant="outline"
                          title="Edit user"
                          aria-label="Edit user"
                          onClick={() => openEdit(u)}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          type="button"
                          size="icon"
                          variant="secondary"
                          title={u.isActive ? 'Deactivate user' : 'Activate user'}
                          aria-label={u.isActive ? 'Deactivate user' : 'Activate user'}
                          disabled={busy}
                          onClick={() => toggleActive(u)}
                        >
                          {u.isActive ? <UserX className="h-4 w-4" /> : <Check className="h-4 w-4" />}
                        </Button>
                        <Button
                          type="button"
                          size="icon"
                          variant="destructive"
                          title="Delete user"
                          aria-label="Delete user"
                          disabled={busy}
                          onClick={() => removeUser(u)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </td>
                  )}

                </tr>

              ))}

              {pag.total === 0 && (
                <tr>
                  <td colSpan={adminOnly ? 7 : 6} className="py-6 text-center text-muted-foreground">
                    No users match your filters.
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

        <Dialog open={!!modal} onOpenChange={(o) => !o && setModal(null)}>

          <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">

            <DialogHeader>

              <DialogTitle>{modal?.mode === 'add' ? 'Add user' : 'Edit user'}</DialogTitle>

            </DialogHeader>

            <div className="space-y-3 py-2">

              <div className="space-y-2">

                <Label>Username</Label>

                <Input

                  value={form.username}

                  onChange={(e) => updateForm('username', e.target.value)}

                  autoComplete="username"
                  disabled={modal?.mode === 'edit'}

                />
                {modal?.mode === 'edit' && (
                  <p className="text-xs text-muted-foreground">Username cannot be changed after creation.</p>
                )}

              </div>

              <div className="space-y-2">

                <Label>Email</Label>

                <Input

                  type="email"

                  value={form.email}

                  onChange={(e) => updateForm('email', e.target.value)}

                  autoComplete="off"

                />

              </div>

              <div className="space-y-2">

                <Label>Full name</Label>

                <Input

                  value={form.fullName}

                  onChange={(e) => updateForm('fullName', e.target.value)}

                  autoComplete="off"

                />

              </div>
              <div className="space-y-2">
                <Label>Location</Label>
                <Select value={form.location} onChange={(e) => updateForm('location', e.target.value)}>
                  <option value="">Select location…</option>
                  {locations.map((loc) => (
                    <option key={loc.id ?? loc.code} value={loc.code}>
                      {loc.code}
                      {loc.name ? ` — ${loc.name}` : ''}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Extension (optional)</Label>
                <Input
                  value={form.extension}
                  onChange={(e) => updateForm('extension', e.target.value)}
                  autoComplete="off"
                />
              </div>

              <div className="space-y-2">

                <Label>Role</Label>

                <Select value={form.role} onChange={(e) => updateForm('role', e.target.value)}>

                  {ROLE_OPTIONS.map((r) => (

                    <option key={r.value} value={r.value}>

                      {r.label}

                    </option>

                  ))}

                </Select>

              </div>

              <div className="space-y-2">

                <Label>Company</Label>

                <Select value={form.companySel} onChange={(e) => updateForm('companySel', e.target.value)}>

                  <option value="__none__">No company</option>

                  {companies.map((c) => (

                    <option key={c.id} value={String(c.id)}>

                      {c.name} ({c.companyType})

                    </option>

                  ))}

                </Select>

              </div>

              {showCarrierApprovalField && (
                <div className="space-y-2">
                  <Label>Carrier approval status</Label>
                  <Select
                    value={form.carrierStatusSel}
                    onChange={(e) => updateForm('carrierStatusSel', e.target.value)}
                  >
                    {modal?.mode === 'edit' && <option value="__keep__">Unchanged</option>}
                    {modal?.mode === 'edit' && <option value="__none__">Clear (null)</option>}
                    {modal?.mode === 'add' && <option value="__none__">None</option>}
                    {CARRIER_STATUS.map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </Select>
                </div>
              )}

              <div className="space-y-2">

                <Label>{modal?.mode === 'add' ? 'Password' : 'New password'}</Label>

                <Input

                  type="password"

                  placeholder={modal?.mode === 'add' ? 'Required' : 'Leave blank to keep current'}

                  value={form.password}

                  onChange={(e) => updateForm('password', e.target.value)}

                  autoComplete="new-password"

                />
              </div>
              <div className="space-y-2">
                <Label>{modal?.mode === 'add' ? 'Confirm password' : 'Confirm new password'}</Label>
                <Input
                  type="password"
                  placeholder={modal?.mode === 'add' ? 'Retype password' : 'Retype new password'}
                  value={form.confirmPassword}
                  onChange={(e) => updateForm('confirmPassword', e.target.value)}
                  autoComplete="new-password"
                />
              </div>
              <div className="text-xs space-y-1">
                <p className={pwChecks.min8 ? 'text-emerald-700' : 'text-amber-700'}>Must have 8 characters minimum</p>
                <p className={pwChecks.hasAlpha ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one alpha</p>
                <p className={pwChecks.hasNumeric ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one numeric</p>
                <p className={pwChecks.hasUpper ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one uppercase</p>
                <p className={pwChecks.hasLower ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one lower case</p>
              </div>

              {msg && <p className="text-sm text-destructive">{msg}</p>}

            </div>

            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="outline" onClick={() => setModal(null)}>
                Cancel
              </Button>
              <Button type="button" disabled={busy} onClick={() => saveModal()}>
                {busy ? (modal?.mode === 'add' ? 'Creating…' : 'Saving…') : modal?.mode === 'add' ? 'Create user' : 'Save'}
              </Button>
            </div>

          </DialogContent>

        </Dialog>

      </CardContent>

    </Card>

  );

}


