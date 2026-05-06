'use client';
import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  LayoutDashboard,
  Loader2,
  Truck,
  FileText,
  Users,
  Building2,
  ClipboardList,
  ShoppingBag,
  LogOut,
} from 'lucide-react';
import { Api } from '@/lib/api';
import { ROLES } from '@/lib/constants';
import { canAccessCarrierPortal, canCreateLoads } from '@/lib/permissions';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { UserContext, type UserSession } from '@/lib/user-context';

const NAV: Array<{ href: string; label: string; icon: any; roles?: string[] }> = [
  { href: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/loads', label: 'Loads', icon: Truck },
  { href: '/templates', label: 'Templates', icon: FileText },
  { href: '/portal', label: 'Carrier portal', icon: ShoppingBag },
  { href: '/claims', label: 'Claims', icon: ClipboardList },
  { href: '/companies', label: 'Companies', icon: Building2, roles: [ROLES.Admin] },
  { href: '/users', label: 'Users', icon: Users, roles: [ROLES.Admin] },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [session, setSession] = useState<UserSession>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const me = await Api.me();
      if (cancelled) return;
      if (!me?.user) {
        router.replace('/login');
        return;
      }
      setSession({ user: me.user, company: me.company });
      setLoading(false);
    })();
    return () => {
      cancelled = true;
    };
  }, [router]);

  async function logout() {
    await Api.logout();
    router.replace('/login');
  }

  if (loading || !session)
    return (
      <div className="min-h-screen grid place-items-center text-muted-foreground">
        <div className="flex items-center gap-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span>Loading your workspace…</span>
        </div>
      </div>
    );

  const nav = NAV.filter((n) => {
    if (n.href === '/portal') return canAccessCarrierPortal(session.user as any, session.company);
    if (n.href === '/templates') return canCreateLoads(session.user as any, session.company);
    if (!n.roles) return true;
    return n.roles.includes(session.user.role);
  });

  const pageTitle = nav.find((n) => pathname === n.href || pathname?.startsWith(n.href + '/'))?.label || 'Dashboard';

  return (
    <UserContext.Provider value={session}>
      <div className="min-h-screen grid grid-cols-[240px_1fr] bg-gradient-to-br from-slate-50 via-sky-50/40 to-white">
        <aside
          className={cn(
            'flex flex-col border-r shadow-[4px_0_32px_-8px_rgba(2,6,23,0.45)]',
            'border-sky-950/60 bg-gradient-to-b from-[#0a1628] via-[#0c1f35] to-[#071018]',
          )}
        >
          <div className="relative shrink-0 px-4 py-4 border-b border-white/[0.08]">
            <div
              className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-sky-400 via-cyan-400 to-sky-500"
              aria-hidden
            />
            <div className="font-bold text-[13px] tracking-tight text-sky-200 uppercase mt-2">americanloads</div>
            <div className="text-[11px] font-medium text-sky-400/65 truncate mt-0.5">{session.company?.name || 'Loadboard'}</div>
          </div>
          <nav className="flex-1 py-3 px-2 space-y-0.5">
            {nav.map((n) => {
              const Icon = n.icon;
              const active = pathname === n.href || pathname?.startsWith(n.href + '/');
              return (
                <Link
                  key={n.href}
                  href={n.href}
                  className={cn(
                    'group flex items-center gap-2 rounded-lg px-2.5 py-2 text-[13px] transition-colors',
                    'hover:bg-white/[0.07] hover:text-sky-100',
                    active
                      ? 'bg-sky-500/20 text-white font-semibold border border-sky-400/30 shadow-[inset_0_1px_0_0_rgba(125,211,252,0.12)]'
                      : 'text-slate-400',
                  )}
                >
                  <Icon className={cn('h-[15px] w-[15px] shrink-0', active ? 'text-sky-300' : 'text-slate-500 group-hover:text-sky-400')} />
                  {n.label}
                </Link>
              );
            })}
          </nav>
          <div className="shrink-0 p-3 border-t border-white/[0.08] bg-black/20">
            <div className="px-2.5 py-2 mb-2 rounded-lg border border-white/[0.1] bg-white/[0.04] text-[11px] backdrop-blur-sm">
              <div className="font-semibold truncate text-sky-50">{session.user.fullName}</div>
              <div className="text-sky-400/65 capitalize tracking-wide">{session.user.role}</div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="w-full justify-start gap-2 text-xs text-slate-300 hover:bg-white/[0.08] hover:text-white"
              onClick={logout}
            >
              <LogOut className="h-3.5 w-3.5" />
              Sign out
            </Button>
          </div>
        </aside>
        <main className="min-w-0 flex flex-col overflow-x-hidden">
          <header
            className={cn(
              'shrink-0 h-12 px-5 flex items-center justify-between gap-4',
              'border-b border-white/[0.08]',
              'bg-gradient-to-r from-[#0a1628] via-[#0c1f35] to-[#0b1a30]',
              'shadow-[0_8px_24px_-16px_rgba(2,6,23,0.5)]',
            )}
          >
            <div className="min-w-0 flex items-baseline gap-3">
              <span className="hidden sm:inline text-[10px] font-semibold uppercase tracking-widest text-sky-400/55">
                Operations
              </span>
              <h1 className="text-[15px] font-semibold leading-none text-sky-50 truncate capitalize">{pageTitle}</h1>
            </div>
            <div className="flex items-center gap-3 shrink-0">
              <span className="hidden md:inline max-w-[200px] truncate text-[11px] text-sky-400/55">
                {session.user.username || session.user.email}
              </span>
              {session.company && (
                <div className="flex items-center gap-2 rounded-full border border-white/15 bg-black/25 pl-3 pr-1.5 py-1 backdrop-blur-sm">
                  <span className="max-w-[160px] truncate text-[11px] font-semibold text-sky-100">{session.company.name}</span>
                  <span
                    className={cn(
                      'rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide',
                      session.company.onboardingStatus === 'approved' && 'bg-emerald-500/20 text-emerald-300 ring-1 ring-emerald-500/30',
                      session.company.onboardingStatus === 'pending' && 'bg-amber-500/20 text-amber-200 ring-1 ring-amber-500/30',
                      session.company.onboardingStatus === 'rejected' && 'bg-rose-500/20 text-rose-200 ring-1 ring-rose-500/30',
                      session.company.onboardingStatus === 'suspended' && 'bg-slate-500/25 text-slate-300 ring-1 ring-white/15',
                    )}
                  >
                    {session.company.onboardingStatus}
                  </span>
                </div>
              )}
            </div>
          </header>
          <div className="h-px w-full shrink-0 bg-gradient-to-r from-transparent via-sky-400/55 to-transparent" aria-hidden />
          <div className="flex-1 p-5">{children}</div>
        </main>
      </div>
    </UserContext.Provider>
  );
}
