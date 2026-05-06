'use client';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Api } from '@/lib/api';
import { evaluatePasswordPolicy, isPasswordPolicyValid } from '@/lib/password-policy';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select } from '@/components/ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

export default function RegisterPage() {
  const [locations, setLocations] = useState<Array<{ id?: number; code: string; name: string }>>([]);
  const [form, setForm] = useState({
    username: '',
    fullName: '',
    location: '',
    extension: '',
    email: '',
    password: '',
    confirmPassword: '',
    companyName: '',
    companyType: 'shipper',
  });
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    Api.registerLocations()
      .then((rows) => {
        setLocations(rows);
        if (rows.length > 0) {
          setForm((p) => ({ ...p, location: p.location || rows[0].code }));
        }
      })
      .catch(() => setLocations([]));
  }, []);

  function update<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((p) => ({ ...p, [k]: v }));
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (form.password !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (!isPasswordPolicyValid(form.password)) {
      setError('Password does not meet required rules.');
      return;
    }
    if (!form.location) {
      setError('Location is required.');
      return;
    }
    setLoading(true);
    try {
      await Api.register({
        username: form.username.trim(),
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        password: form.password,
        location: form.location,
        extension: form.extension.trim() || undefined,
        companyName: form.companyName.trim(),
        companyType: form.companyType,
      });
      setDone(true);
    } catch (err: any) {
      setError(err.message || 'Registration failed');
    } finally {
      setLoading(false);
    }
  }

  const pw = evaluatePasswordPolicy(form.password);

  return (
    <div className="min-h-screen grid place-items-center bg-gradient-to-br from-slate-50 to-slate-100 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">Create your account</CardTitle>
          <CardDescription>
            Enter your organization — we create your company as <b>pending</b> until an admin approves it.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {done ? (
            <div className="space-y-4">
              <p className="text-sm">
                Account created. Your company onboarding status is <b>pending</b> approval.
              </p>
              <Button asChild className="w-full">
                <Link href="/login">Go to login</Link>
              </Button>
            </div>
          ) : (
            <form onSubmit={onSubmit} className="space-y-3">
              <div className="space-y-2">
                <Label>Username (you will sign in with this)</Label>
                <Input
                  value={form.username}
                  onChange={(e) => update('username', e.target.value)}
                  autoComplete="username"
                  required
                  minLength={3}
                />
              </div>
              <div className="space-y-2">
                <Label>Full name</Label>
                <Input value={form.fullName} onChange={(e) => update('fullName', e.target.value)} required />
              </div>
              <div className="space-y-2">
                <Label>Email</Label>
                <Input
                  type="email"
                  value={form.email}
                  onChange={(e) => update('email', e.target.value)}
                  autoComplete="email"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label>Location</Label>
                <Select value={form.location} onChange={(e) => update('location', e.target.value)} required>
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
                <Input value={form.extension} onChange={(e) => update('extension', e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label>Password</Label>
                <Input type="password" value={form.password} onChange={(e) => update('password', e.target.value)} required />
              </div>
              <div className="space-y-2">
                <Label>Confirm password</Label>
                <Input
                  type="password"
                  value={form.confirmPassword}
                  onChange={(e) => update('confirmPassword', e.target.value)}
                  required
                />
              </div>
              <div className="text-xs space-y-1">
                <p className={pw.min8 ? 'text-emerald-700' : 'text-amber-700'}>Must have 8 characters minimum</p>
                <p className={pw.hasAlpha ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one alpha</p>
                <p className={pw.hasNumeric ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one numeric</p>
                <p className={pw.hasUpper ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one uppercase</p>
                <p className={pw.hasLower ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one lower case</p>
              </div>
              <div className="space-y-2">
                <Label>Company name</Label>
                <Input value={form.companyName} onChange={(e) => update('companyName', e.target.value)} required />
              </div>
              <div className="space-y-2">
                <Label>I am registering as</Label>
                <Select value={form.companyType} onChange={(e) => update('companyType', e.target.value)} required>
                  <option value="shipper">Shipper (post loads)</option>
                  <option value="carrier">Carrier (claim / bid loads)</option>
                </Select>
              </div>
              {error && <p className="text-sm text-destructive">{error}</p>}
              <Button className="w-full" disabled={loading} type="submit">
                {loading ? 'Creating…' : 'Create account'}
              </Button>
              <p className="text-sm text-muted-foreground text-center">
                Already have an account?{' '}
                <Link href="/login" className="text-primary hover:underline">
                  Sign in
                </Link>
              </p>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
