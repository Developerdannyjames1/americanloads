'use client';
import { Suspense, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Api } from '@/lib/api';
import { Loader2 } from 'lucide-react';
import { evaluatePasswordPolicy, isPasswordPolicyValid } from '@/lib/password-policy';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

function ResetForm() {
  const params = useSearchParams();
  const tokenParam = params.get('token') || '';
  const emailParam = params.get('email') || '';

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [token] = useState(tokenParam);
  const [email] = useState(emailParam);
  const [done, setDone] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (!email || !token) {
      setError('Reset link is invalid or incomplete. Please request a new reset email.');
      return;
    }
    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (!isPasswordPolicyValid(password)) {
      setError('Password does not meet required rules.');
      return;
    }
    setLoading(true);
    try {
      await Api.reset({ token, email, password });
      setDone(true);
    } catch (err: any) {
      setError(err.message || 'Reset failed');
    } finally {
      setLoading(false);
    }
  }

  if (done) {
    return (
      <div className="space-y-4">
        <p className="text-sm">Your password has been updated.</p>
        <Button asChild className="w-full">
          <Link href="/login">Sign in</Link>
        </Button>
      </div>
    );
  }

  const pw = evaluatePasswordPolicy(password);

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="space-y-2">
        <Label>Email</Label>
        <Input type="email" required value={email} disabled />
      </div>
      <div className="space-y-2">
        <Label>New password</Label>
        <Input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
      </div>
      <div className="space-y-2">
        <Label>Confirm password</Label>
        <Input
          type="password"
          required
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
        />
      </div>
      <div className="text-xs space-y-1">
        <p className={pw.min8 ? 'text-emerald-700' : 'text-amber-700'}>Must have 8 characters minimum</p>
        <p className={pw.hasAlpha ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one alpha</p>
        <p className={pw.hasNumeric ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one numeric</p>
        <p className={pw.hasUpper ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one uppercase</p>
        <p className={pw.hasLower ? 'text-emerald-700' : 'text-amber-700'}>Must include at least one lower case</p>
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <Button type="submit" className="w-full" disabled={loading}>
        {loading ? (
          <span className="inline-flex items-center gap-2">
            <Loader2 className="h-4 w-4 animate-spin" />
            Updating…
          </span>
        ) : (
          'Update password'
        )}
      </Button>
    </form>
  );
}

export default function ResetPasswordPage() {
  return (
    <div className="min-h-screen grid place-items-center bg-gradient-to-br from-slate-50 to-slate-100 p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">Choose a new password</CardTitle>
          <CardDescription>Use at least 8 characters.</CardDescription>
        </CardHeader>
        <CardContent>
          <Suspense fallback={<div className="text-sm text-muted-foreground">Loading…</div>}>
            <ResetForm />
          </Suspense>
        </CardContent>
      </Card>
    </div>
  );
}
