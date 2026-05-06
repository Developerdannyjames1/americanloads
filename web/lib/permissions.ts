import { ROLES } from './constants';

export type CurrentUser = {
  sub: string;
  role: 'admin' | 'shipper' | 'carrier' | 'dispatcher';
  companyId?: string | null;
  companyPermissions?: {
    canCreateLoads?: boolean;
    canSubmitClaims?: boolean;
    canAccessCarrierPortal?: boolean;
  };
  fullName?: string;
  email?: string;
} | null;

export type CompanyLike = { companyType?: string; onboardingStatus?: string } | null | undefined;

function companyTypeLc(c?: CompanyLike) {
  return String(c?.companyType || '').toLowerCase();
}

export function isAdmin(u: CurrentUser) {
  return !!u && u.role === ROLES.Admin;
}

/** Only admins may edit carrier pay on loads (API enforces the same). */
export function canSetCarrierPay(u: CurrentUser) {
  return isAdmin(u);
}

/** Shippers, staff, and dispatchers employed by a shipper (not carrier) company. */
export function canCreateLoads(u: CurrentUser, company?: CompanyLike) {
  if (!u) return false;
  if (u.companyPermissions && typeof u.companyPermissions.canCreateLoads === 'boolean') {
    return isAdmin(u) || !!u.companyPermissions.canCreateLoads;
  }
  if (isAdmin(u)) return true;
  if (u.role === ROLES.Shipper) return true;
  if (u.role === ROLES.Dispatcher && companyTypeLc(company) === ROLES.Shipper) return true;
  return false;
}

/** Carriers and dispatchers at an approved carrier company (shipper dispatchers use the load board, not claims here). */
export function canSubmitClaims(u: CurrentUser, company?: CompanyLike) {
  if (!u) return false;
  if (u.companyPermissions && typeof u.companyPermissions.canSubmitClaims === 'boolean') {
    return !!u.companyPermissions.canSubmitClaims;
  }
  if (u.role === ROLES.Carrier) return true;
  if (u.role === ROLES.Dispatcher && companyTypeLc(company) === ROLES.Carrier) return true;
  return false;
}

/** Carrier portal: carriers and dispatchers at a carrier company (not admins). */
export function canAccessCarrierPortal(u: CurrentUser, company?: CompanyLike) {
  if (!u || isAdmin(u)) return false;
  if (u.companyPermissions && typeof u.companyPermissions.canAccessCarrierPortal === 'boolean') {
    return !!u.companyPermissions.canAccessCarrierPortal;
  }
  if (u.role === ROLES.Carrier) return true;
  if (u.role === ROLES.Dispatcher && companyTypeLc(company) === ROLES.Carrier) return true;
  return false;
}

/** Accept/reject claims and bids: administrators only (API enforces the same). */
export function canAcceptRejectClaims(u: CurrentUser, _company?: CompanyLike) {
  return isAdmin(u);
}
/**
 * In transit & delivered: assigned carrier or dispatcher at that carrier company (same as API).
 * Administrators use Loads workflow for overrides plus Completed.
 */
export function canAdvanceCarrierLeg(u: CurrentUser, load: any, company?: CompanyLike) {
  if (!u || !load) return false;
  const aid = load.assignedCarrierUserId;
  if (!aid) return false;
  if (u.role === ROLES.Carrier && String(aid) === u.sub) return true;
  if (u.role === ROLES.Dispatcher && companyTypeLc(company) === ROLES.Carrier) {
    const cc = load.assignedCarrier?.companyId;
    if (cc != null && String(cc) === String(u.companyId)) return true;
  }
  return false;
}

/** Completed is admin-only (API enforces). */
export function canMarkLoadCompleted(u: CurrentUser) {
  return isAdmin(u);
}

/** Show In transit / Delivered controls (carrier leg). Admins always see these for operations. */
export function canUseCarrierLegWorkflow(u: CurrentUser, load: any, company?: CompanyLike) {
  return isAdmin(u) || canAdvanceCarrierLeg(u, load, company);
}
export function canCancelLoad(u: CurrentUser, load: any) {
  if (!u || !load) return false;
  if (isAdmin(u)) return true;
  return u.role === ROLES.Shipper && String(load.shipperUserId) === u.sub;
}
