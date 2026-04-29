using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ASTDAT.Data.Models;
using ASTDAT.Web.Infrastructure;
using ASTDAT.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace ASTDAT.Web.Controllers.API
{
    [Authorize]
    [RoutePrefix("api/LoadClaims")]
    public class LoadClaimsController : ApiController
    {
        const string ClaimTypeClaim = "claim";
        const string ClaimTypeBid = "bid";
        const string ClaimStatusPending = "pending";
        const string ClaimStatusAccepted = "accepted";
        const string ClaimStatusRejected = "rejected";

        public class LoadClaimSubmitModel
        {
            public int LoadId { get; set; }
            public string ClaimType { get; set; }
            public decimal? BidAmount { get; set; }
            public string Message { get; set; }
        }

        public class ClaimIdModel
        {
            public int Id { get; set; }
        }

        [Route("Submit")]
        [HttpPost]
        public IHttpActionResult Submit(LoadClaimSubmitModel m)
        {
            if (m == null) return BadRequest();
            if (!LoadboardPermissions.CanClaimOrBidLoads(HttpContext.Current.User))
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Forbidden, "Claims/bids are for approved carrier accounts."));

            var uid = User.Identity.GetUserId();
            var type = (m.ClaimType ?? "").Trim().ToLowerInvariant();
            if (type != ClaimTypeClaim && type != ClaimTypeBid)
                return BadRequest("Type must be 'claim' or 'bid'.");
            if (type == ClaimTypeBid && !m.BidAmount.HasValue)
                return BadRequest("Bid amount is required for bids.");

            using (var db = new DBContext())
            {
                var load = db.Loads.FirstOrDefault(x => x.Id == m.LoadId);
                if (load == null) return NotFound();

                var ws = string.IsNullOrEmpty(load.WorkflowStatus) ? LoadWorkflowStatuses.Posted : load.WorkflowStatus;
                if (string.Equals(ws, LoadWorkflowStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Load is not posted yet.");
                if (string.Equals(ws, LoadWorkflowStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Load is cancelled.");
                if (string.Equals(ws, LoadWorkflowStatuses.Assigned, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ws, LoadWorkflowStatuses.InTransit, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ws, LoadWorkflowStatuses.Delivered, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ws, LoadWorkflowStatuses.Completed, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Load is not open for new claims.");

                var hasPending = db.LoadClaims.Any(c => c.LoadId == load.Id && c.CarrierUserId == uid && c.Status == ClaimStatusPending);
                if (hasPending) return BadRequest("You already have a pending claim/bid for this load.");

                var claim = new LoadClaimModel
                {
                    LoadId = load.Id,
                    CarrierUserId = uid,
                    ClaimType = type,
                    BidAmount = m.BidAmount,
                    Message = m.Message,
                    Status = ClaimStatusPending,
                    CreatedUtc = DateTime.UtcNow
                };
                db.LoadClaims.Add(claim);

                if (string.IsNullOrEmpty(load.WorkflowStatus)
                    || string.Equals(load.WorkflowStatus, LoadWorkflowStatuses.Posted, StringComparison.OrdinalIgnoreCase))
                {
                    load.WorkflowStatus = LoadWorkflowStatuses.Claimed;
                }

                db.SaveChanges();
                LoadNotificationService.OnClaimOrBidSaved(load.Id, claim.Id, type);
                LoadNotificationService.NotifyAdminsNewClaimOrBid(load.Id, claim.Id, type);
                return Ok(new { Ok = true, Id = claim.Id });
            }
        }

        [Route("ListForLoad/{loadId:int}")]
        [HttpGet]
        public IHttpActionResult ListForLoad(int loadId)
        {
            if (!LoadboardPermissions.CanViewLoads(HttpContext.Current.User))
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Forbidden));

            using (var db = new DBContext())
            {
                var load = db.Loads.AsNoTracking().FirstOrDefault(x => x.Id == loadId);
                if (load == null) return NotFound();
                if (!LoadboardPermissions.CanViewClaimsForLoad(HttpContext.Current.User, load))
                    return ResponseMessage(Request.CreateResponse(HttpStatusCode.Forbidden, "You cannot view claims for this load."));

                var rows = db.LoadClaims.AsNoTracking()
                    .Where(c => c.LoadId == loadId)
                    .OrderByDescending(c => c.CreatedUtc)
                    .ToList();
                return Ok(rows);
            }
        }

        [Route("Accept")]
        [HttpPost]
        public IHttpActionResult Accept(ClaimIdModel m)
        {
            if (!LoadboardPermissions.CanManageLoadClaimsAsAdmin(HttpContext.Current.User))
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Forbidden, "Only administrators can accept claims."));

            var uid = User.Identity.GetUserId();
            using (var db = new DBContext())
            {
                var claim = db.LoadClaims.FirstOrDefault(c => c.Id == m.Id);
                if (claim == null) return NotFound();
                if (claim.Status != ClaimStatusPending)
                    return BadRequest("Claim is not pending.");

                var load = db.Loads.FirstOrDefault(x => x.Id == claim.LoadId);
                if (load == null) return NotFound();

                var now = DateTime.UtcNow;
                var rejectedCarriers = new List<string>();
                foreach (var c in db.LoadClaims.Where(x => x.LoadId == load.Id && x.Status == ClaimStatusPending).ToList())
                {
                    if (c.Id == claim.Id)
                    {
                        c.Status = ClaimStatusAccepted;
                        c.ResolvedUtc = now;
                        c.ResolvedByUserId = uid;
                    }
                    else
                    {
                        c.Status = ClaimStatusRejected;
                        c.ResolvedUtc = now;
                        c.ResolvedByUserId = uid;
                        if (!string.IsNullOrEmpty(c.CarrierUserId))
                            rejectedCarriers.Add(c.CarrierUserId);
                    }
                }

                load.AssignedCarrierUserId = claim.CarrierUserId;
                load.WorkflowStatus = LoadWorkflowStatuses.Assigned;
                load.UpdateDate = DateTime.Now;
                load.UpdatedBy = User.Identity.Name;
                db.SaveChanges();

                foreach (var r in rejectedCarriers.Distinct(StringComparer.Ordinal))
                    LoadNotificationService.NotifyCarrierClaimRejected(r, load.Id);

                var load2 = db.Loads
                    .Include(x => x.Origin)
                    .Include(x => x.Destination)
                    .FirstOrDefault(x => x.Id == load.Id);
                try
                {
                    if (load2 != null)
                    {
                        var o = load2.Origin?.City;
                        var d = load2.Destination?.City;
                        LoadNotificationService.OnLoadAssigned(load2.Id, claim.CarrierUserId);
                        LoadNotificationService.OnWorkflowChanged(load2.Id, LoadWorkflowStatuses.Assigned);
                        LoadNotificationService.NotifyLoadAssignedToParties(load2.Id, load2.ShipperUserId, claim.CarrierUserId, load2.ClientName, o, d);
                    }
                }
                catch
                {
                    // ignore notification failures
                }
                return Ok(new { Ok = true });
            }
        }

        [Route("Reject")]
        [HttpPost]
        public IHttpActionResult Reject(ClaimIdModel m)
        {
            if (!LoadboardPermissions.CanManageLoadClaimsAsAdmin(HttpContext.Current.User))
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.Forbidden, "Only administrators can reject claims."));

            var uid = User.Identity.GetUserId();
            using (var db = new DBContext())
            {
                var claim = db.LoadClaims.FirstOrDefault(c => c.Id == m.Id);
                if (claim == null) return NotFound();
                if (claim.Status != ClaimStatusPending)
                    return BadRequest("Claim is not pending.");

                claim.Status = ClaimStatusRejected;
                claim.ResolvedUtc = DateTime.UtcNow;
                claim.ResolvedByUserId = uid;

                var loadId = claim.LoadId;
                if (!db.LoadClaims.Any(c => c.LoadId == loadId && c.Status == ClaimStatusPending))
                {
                    var load = db.Loads.FirstOrDefault(x => x.Id == loadId);
                    if (load != null
                        && string.Equals(load.WorkflowStatus, LoadWorkflowStatuses.Claimed, StringComparison.OrdinalIgnoreCase))
                    {
                        load.WorkflowStatus = LoadWorkflowStatuses.Posted;
                        load.UpdateDate = DateTime.Now;
                        load.UpdatedBy = User.Identity.Name;
                    }
                }

                db.SaveChanges();
                LoadNotificationService.OnClaimRejected(loadId, claim.Id, claim.CarrierUserId);
                LoadNotificationService.NotifyCarrierClaimRejected(claim.CarrierUserId, loadId);
                return Ok(new { Ok = true });
            }
        }
    }
}
