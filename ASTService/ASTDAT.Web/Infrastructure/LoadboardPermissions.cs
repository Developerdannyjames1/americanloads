using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using ASTDAT.Data.Models;
using ASTDAT.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace ASTDAT.Web.Infrastructure
{
    /// <summary>Role + company based access. One role name: Dispatcher (legacy DB name "Dispatch" is treated the same until merged). Internal staff: Manager or Dispatcher with no company.</summary>
    public static class LoadboardPermissions
    {
        public const string RoleAdmin = "Admin";
        public const string RoleShipper = "Shipper";
        public const string RoleCarrier = "Carrier";
        public const string RoleDispatcher = "Dispatcher";
        public const string RoleManager = "Manager";
        /// <summary>Legacy role name in some databases; treat as <see cref="RoleDispatcher"/> for permissions.</summary>
        public const string RoleDispatchLegacy = "Dispatch";

        public const string CompanyTypeShipper = "Shipper";
        public const string CompanyTypeCarrier = "Carrier";

        public static readonly string[] AllKnownRoles = { RoleAdmin, RoleShipper, RoleCarrier, RoleDispatcher, RoleManager };

        public static bool HasDispatcherOrLegacyDispatch(IList<string> roles) =>
            roles != null && (roles.Contains(RoleDispatcher) || roles.Contains(RoleDispatchLegacy));

        /// <summary>Admin, Manager, legacy Dispatch, or Dispatcher with no company (internal staff — full loadboard).</summary>
        public static bool IsInternalStaffForLoadboard(string userId, IList<string> roles)
        {
            if (roles == null) return false;
            if (roles.Contains(RoleAdmin) || roles.Contains(RoleManager)) return true;
            if (!HasDispatcherOrLegacyDispatch(roles)) return false;
            var user = string.IsNullOrEmpty(userId) ? null : GetUser(userId);
            // Dispatcher or legacy "Dispatch": internal only when not tied to a company.
            return user == null || user.CompanyId == null;
        }

        public static ApplicationUserManager GetUserManager() =>
            HttpContext.Current?.GetOwinContext()?.GetUserManager<ApplicationUserManager>();

        public static IList<string> GetRoleNames(string userId)
        {
            var userManager = GetUserManager();
            if (userManager == null) return new List<string>();
            return userManager.GetRolesAsync(userId).GetAwaiter().GetResult().ToList();
        }

        public static ApplicationUser GetUser(string userId) => GetUserManager()?.FindById(userId);

        public static Company GetCompanyForUser(ApplicationUser user, ApplicationDbContext appDb = null)
        {
            if (user?.CompanyId == null) return null;
            if (appDb == null) appDb = new ApplicationDbContext();
            return appDb.Companies.Find(user.CompanyId);
        }

        public static string EffectiveOnboardingStatus(ApplicationUser user, Company company)
        {
            if (user == null) return null;
            if (company != null) return OnboardingStatuses.Normalize(company.OnboardingStatus) ?? OnboardingStatuses.Pending;
            if (user.Company == null) return null;
            return OnboardingStatuses.Normalize(user.Company.OnboardingStatus) ?? OnboardingStatuses.Pending;
        }

        static bool IsUserCarrierLineApprovedAsLegacy(ApplicationUser user)
        {
            if (user == null) return false;
            if (string.IsNullOrEmpty(user.CarrierApprovalStatus)) return false;
            if (string.Equals(user.CarrierApprovalStatus, "Pending", StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(user.CarrierApprovalStatus, "pending", StringComparison.Ordinal)) return false;
            return string.Equals(user.CarrierApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(user.CarrierApprovalStatus, "approved", StringComparison.OrdinalIgnoreCase)
                   || OnboardingStatuses.IsApprovedState(OnboardingStatuses.Normalize(user.CarrierApprovalStatus));
        }

        public static bool CompanyAllowsLoadboardRead(ApplicationUser user, IList<string> roles, Company company)
        {
            if (user == null) return false;
            if (company == null)
            {
                if (roles.Contains(RoleCarrier)) return IsUserCarrierLineApprovedAsLegacy(user);
                if (HasDispatcherOrLegacyDispatch(roles) && (user == null || user.CompanyId == null)) return true;
                if (HasDispatcherOrLegacyDispatch(roles) && user != null && user.CompanyId != null) return false;
                if (roles.Contains(RoleShipper)) return true;
                if (!roles.Any(r => r == RoleShipper || r == RoleCarrier || r == RoleDispatcher || r == RoleDispatchLegacy)) return true;
                return true;
            }

            var st = OnboardingStatuses.Normalize(company.OnboardingStatus);
            if (st == OnboardingStatuses.Suspended) return false;
            if (st == OnboardingStatuses.Rejected) return false;
            if (roles.Contains(RoleCarrier) || (HasDispatcherOrLegacyDispatch(roles) && (company.CompanyType ?? "") == CompanyTypeCarrier))
                return st == null || st == string.Empty || OnboardingStatuses.IsApprovedState(st) || st == OnboardingStatuses.NeedsReview;

            if (roles.Contains(RoleShipper) || (HasDispatcherOrLegacyDispatch(roles) && (company.CompanyType ?? "") == CompanyTypeShipper))
            {
                return st == null || st == string.Empty
                    || st == OnboardingStatuses.Approved
                    || st == OnboardingStatuses.NeedsReview
                    || st == OnboardingStatuses.Pending;
            }
            return st == null || OnboardingStatuses.IsApprovedState(st) || st == OnboardingStatuses.NeedsReview || st == OnboardingStatuses.Pending;
        }

        public static bool IsAdminOnly(IList<string> roles) =>
            roles != null && roles.Contains(RoleAdmin);

        public static bool CanApproveUsersAndCompanies(IPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var id = principal.Identity.GetUserId();
            if (string.IsNullOrEmpty(id)) return false;
            return IsAdminOnly(GetRoleNames(id));
        }

        public static bool CanViewLoads(IPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var id = principal.Identity.GetUserId();
            if (string.IsNullOrEmpty(id)) return false;
            var roles = GetRoleNames(id);
            if (IsInternalStaffForLoadboard(id, roles)) return true;
            if (roles.Contains(RoleAdmin)) return true;

            var userManager = GetUserManager();
            if (userManager == null) return false;
            var user = userManager.FindById(id);
            if (user == null) return false;
            if (!roles.Any(r => r == RoleShipper || r == RoleCarrier || r == RoleDispatcher || r == RoleDispatchLegacy)) return true;

            using (var appDb = new ApplicationDbContext())
            {
                var c = user.CompanyId == null ? null : appDb.Companies.Find(user.CompanyId);
                if (!CompanyAllowsLoadboardRead(user, roles, c)) return false;
            }
            return true;
        }

        public static bool CanCreateOrEditLoads(IPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var id = principal.Identity.GetUserId();
            if (string.IsNullOrEmpty(id)) return false;
            var roles = GetRoleNames(id);
            if (IsInternalStaffForLoadboard(id, roles)) return true;
            if (roles.Contains(RoleAdmin)) return true;
            if (roles.Contains(RoleCarrier)) return false;
            if (!roles.Any(r => r == RoleShipper || r == RoleDispatcher || r == RoleDispatchLegacy)) return true;

            var userManager = GetUserManager();
            if (userManager == null) return false;
            var user = userManager.FindById(id);
            if (user == null) return false;
            if (user.CompanyId == null) return true;

            using (var appDb = new ApplicationDbContext())
            {
                var c = appDb.Companies.Find(user.CompanyId);
                if (c == null) return true;
                if (c.CompanyType != CompanyTypeShipper) return false;
                var st = OnboardingStatuses.Normalize(c.OnboardingStatus);
                if (st == OnboardingStatuses.Suspended || st == OnboardingStatuses.Rejected) return false;
                if (st == null || st == string.Empty) return true;
                return st == OnboardingStatuses.Approved || st == OnboardingStatuses.NeedsReview || st == OnboardingStatuses.Pending;
            }
        }

        public static bool CanClaimOrBidLoads(IPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var id = principal.Identity.GetUserId();
            if (string.IsNullOrEmpty(id)) return false;
            var roles = GetRoleNames(id);
            if (IsInternalStaffForLoadboard(id, roles) || roles.Contains(RoleAdmin)) return false;
            if (!CanViewLoads(principal)) return false;
            if (roles.Contains(RoleCarrier))
                return true;
            if (HasDispatcherOrLegacyDispatch(roles))
            {
                var user = GetUser(id);
                if (user?.CompanyId == null) return false;
                using (var appDb = new ApplicationDbContext())
                {
                    var c = appDb.Companies.Find(user.CompanyId);
                    if (c == null) return false;
                    return (c.CompanyType ?? "") == CompanyTypeCarrier
                           && OnboardingStatuses.IsApprovedState(c.OnboardingStatus);
                }
            }
            return false;
        }

        public static bool CanAssignLoadsInProcurement(IPrincipal principal) =>
            principal?.Identity?.IsAuthenticated == true
            && IsAdminOnly(GetRoleNames(principal.Identity.GetUserId()));

        public static bool CanSetCarrierPay(IPrincipal principal) =>
            principal?.Identity?.IsAuthenticated == true
            && IsAdminOnly(GetRoleNames(principal.Identity.GetUserId()));

        public static bool CanSetBilledToCustomer(IPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var roles = GetRoleNames(principal.Identity.GetUserId());
            return IsAdminOnly(roles) || roles.Contains(RoleShipper);
        }

        public static bool HideDraftLoadsFromList(IList<string> roles, string userId)
        {
            if (roles == null) return true;
            if (roles.Contains(RoleAdmin) || roles.Contains(RoleManager)) return false;
            if (roles.Contains(RoleShipper)) return false;
            if (IsInternalStaffForLoadboard(userId, roles)) return false;
            return true;
        }

        public static bool CanPostDraftLoad(IPrincipal principal, LoadModel load)
        {
            if (principal?.Identity?.IsAuthenticated != true || load == null) return false;
            var id = principal.Identity.GetUserId();
            var roles = GetRoleNames(id);
            if (IsAdminOnly(roles) || IsInternalStaffForLoadboard(id, roles)) return true;
            return roles.Contains(RoleShipper) && load.ShipperUserId == id;
        }

        public static bool CanManageLoadClaimsAsAdmin(IPrincipal principal) =>
            principal?.Identity?.IsAuthenticated == true
            && IsAdminOnly(GetRoleNames(principal.Identity.GetUserId()));

        public static bool CanViewClaimsForLoad(IPrincipal principal, LoadModel load)
        {
            if (principal?.Identity?.IsAuthenticated != true || load == null) return false;
            var id = principal.Identity.GetUserId();
            var roles = GetRoleNames(id);
            if (CanManageLoadClaimsAsAdmin(principal)) return true;
            return roles.Contains(RoleShipper) && load.ShipperUserId == id;
        }

        public static bool CanUpdateAssignedExecutionStatus(IPrincipal principal, string assignedCarrierUserId)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var id = principal.Identity.GetUserId();
            if (string.IsNullOrEmpty(id)) return false;
            if (!string.IsNullOrEmpty(assignedCarrierUserId) && string.Equals(assignedCarrierUserId, id, StringComparison.Ordinal))
                return true;
            var roles = GetRoleNames(id);
            return IsAdminOnly(roles) || IsInternalStaffForLoadboard(id, roles);
        }

        /// <summary>Carrier-line users: carrier role or company dispatcher for a carrier company. Staff/internal use the full load board instead.</summary>
        public static bool ShouldUseCarrierPortal(IPrincipal principal)
        {
            if (principal?.Identity?.IsAuthenticated != true) return false;
            var id = principal.Identity.GetUserId();
            if (string.IsNullOrEmpty(id)) return false;
            var roles = GetRoleNames(id);
            if (roles.Contains(RoleAdmin) || roles.Contains(RoleManager)) return false;
            if (IsInternalStaffForLoadboard(id, roles)) return false;
            if (roles.Contains(RoleShipper)) return false;
            if (roles.Contains(RoleCarrier)) return true;
            if (HasDispatcherOrLegacyDispatch(roles))
            {
                var u = GetUser(id);
                if (u?.CompanyId == null) return false;
                var c = GetCompanyForUser(u);
                return c != null && (c.CompanyType ?? "") == CompanyTypeCarrier;
            }
            return false;
        }
    }

    public static class CarrierShipperAccess
    {
        public static bool LegacyPascalCompatApproved(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            return s.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                   || s.Equals("approved", StringComparison.Ordinal);
        }

        [Obsolete("Use LoadboardPermissions.CanViewLoads")]
        public static bool CanAccessLoads(IPrincipal principal) => LoadboardPermissions.CanViewLoads(principal);

        [Obsolete("Use LoadboardPermissions.CanApproveUsersAndCompanies")]
        public static bool CanApproveCarriers(IPrincipal principal) => LoadboardPermissions.CanApproveUsersAndCompanies(principal);
    }

    [Obsolete("Use OnboardingStatuses")]
    public static class CarrierApprovalStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Suspended = "Suspended";

        public static readonly string[] All = { Pending, Approved, Rejected, Suspended };
    }
}
