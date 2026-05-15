// Legacy DB uses PascalCase role/type values. Keep these in sync with what's
// stored in dbo.AspNetRoles.Name and dbo.Companies.CompanyType.

export const Roles = {
  Admin: 'Admin',
  Shipper: 'Shipper',
  Carrier: 'Carrier',
  Dispatcher: 'Dispatcher',
} as const;
export type Role = (typeof Roles)[keyof typeof Roles];

export function normalizeRole(name: string | null | undefined): Role | null {
  if (!name) return null;
  const v = name.trim();
  const map: Record<string, Role> = {
    admin: Roles.Admin,
    shipper: Roles.Shipper,
    carrier: Roles.Carrier,
    dispatcher: Roles.Dispatcher,
    /** Legacy AspNetRoles row merged into Admin for app permissions. */
    manager: Roles.Admin,
  };
  return map[v.toLowerCase()] || null;
}

const PRIMARY_ROLE_ORDER: Role[] = [Roles.Admin, Roles.Shipper, Roles.Carrier, Roles.Dispatcher];

/** Pick canonical role label for users with multiple role rows (AspNetIdentity). */
export function pickPrimaryRole(roleNames: string[]): Role {
  const normalized = roleNames
    .map((n) => normalizeRole(n))
    .filter((r): r is Role => r !== null);
  for (const r of PRIMARY_ROLE_ORDER) if (normalized.includes(r)) return r;
  return normalized[0] || Roles.Carrier;
}

export const CompanyType = {
  Shipper: 'Shipper',
  Carrier: 'Carrier',
} as const;

export const OnboardingStatus = {
  Pending: 'pending',
  Approved: 'approved',
  Rejected: 'rejected',
  Suspended: 'suspended',
  NeedsReview: 'needs_review',
} as const;
export type OnboardingStatusT = (typeof OnboardingStatus)[keyof typeof OnboardingStatus];

// Legacy WorkflowStatus values (lowercase in DB)
export const LoadStatus = {
  Draft: 'draft',
  Posted: 'posted',
  Claimed: 'claimed',
  Assigned: 'assigned',
  InTransit: 'in_transit',
  Delivered: 'delivered',
  Completed: 'completed',
  Cancelled: 'cancelled',
} as const;
export type LoadStatusT = (typeof LoadStatus)[keyof typeof LoadStatus];

export const ClaimType = { Claim: 'claim', Bid: 'bid' } as const;

export const ClaimStatus = {
  Pending: 'pending',
  Accepted: 'accepted',
  Rejected: 'rejected',
} as const;

// OriginDestination.Type — 0 origin, 1 destination
export const ODType = { Origin: 0, Destination: 1 } as const;
