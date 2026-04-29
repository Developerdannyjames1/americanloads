using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using ASTDAT.Data.Models;
using ASTDAT.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace ASTDAT.Web.Infrastructure
{
    /// <summary>Central SMTP + load event log for email + polling clients.</summary>
    public static class LoadNotificationService
    {
        public const string EventClaimSubmitted = "claim_submitted";
        public const string EventLoadAssigned = "load_assigned";
        public const string EventLoadUpdated = "load_updated";
        public const string EventWorkflowChanged = "workflow_changed";
        public const string EventClaimRejected = "claim_rejected";

        public static void OnClaimOrBidSaved(int loadId, int claimId, string claimType) =>
            LoadEventLog.Append(EventClaimSubmitted, new { loadId, claimId, claimType });

        public static void OnLoadAssigned(int loadId, string assignedCarrierUserId) =>
            LoadEventLog.Append(EventLoadAssigned, new { loadId, assignedCarrierUserId });

        public static void OnLoadUpdated(int loadId, string summary) =>
            LoadEventLog.Append(EventLoadUpdated, new { loadId, summary });

        public static void OnWorkflowChanged(int loadId, string newStatus) =>
            LoadEventLog.Append(EventWorkflowChanged, new { loadId, newStatus });

        public static void OnClaimRejected(int loadId, int claimId, string carrierUserId) =>
            LoadEventLog.Append(EventClaimRejected, new { loadId, claimId, carrierUserId });

        public static void NotifyAdminsNewClaimOrBid(int loadId, int claimId, string claimType)
        {
            try
            {
                var body = $"<p>New {claimType} on load <strong>#{loadId}</strong> (request id {claimId}).</p><p>Review in the load board or carrier portal.</p>";
                var subject = $"Load #{loadId} — new carrier {claimType}";
                SendToAllAdmins(subject, body);
            }
            catch { /* ignore */ }
        }

        public static void NotifyCarrierClaimRejected(string carrierUserId, int loadId) =>
            SendToUserId(carrierUserId, $"Load #{loadId} — claim/bid update",
                $"<p>Your claim or bid on load <strong>#{loadId}</strong> was <strong>rejected</strong>.</p>");

        public static void NotifyLoadAssignedToParties(int loadId, string shipperUserId, string winningCarrierUserId, string companyName, string originCity, string destCity)
        {
            var laneText = $"{(originCity ?? "")} → {(destCity ?? "")}".Trim();
            if (string.IsNullOrEmpty(laneText) || laneText == "→") { laneText = "your lane"; }
            var lane = $"<p>{System.Web.HttpUtility.HtmlEncode((originCity ?? ""))} → {System.Web.HttpUtility.HtmlEncode((destCity ?? ""))}</p>";
            var comm = $"<p>Load <strong>#{loadId}</strong>{(string.IsNullOrEmpty(companyName) ? "" : " (" + System.Web.HttpUtility.HtmlEncode(companyName) + ")")} has been assigned to a carrier.</p>";
            try
            {
                SendToUserId(shipperUserId, $"Load #{loadId} — assigned: {laneText}", comm + "<p>Your load has been assigned.</p>" + lane);
            }
            catch { /* */ }
            try
            {
                SendToUserId(winningCarrierUserId, $"Load #{loadId} — you won: {laneText}", comm + "<p>You are assigned to this load. Check the load board for details.</p>" + lane);
            }
            catch { /* */ }
        }

        public static void NotifyWorkflowOrUpdate(int loadId, string newStatus, string messageHtml, string shipperUserId, string assignedCarrierUserId, string updatedBy)
        {
            var subj = string.IsNullOrEmpty(newStatus)
                ? $"Load #{loadId} — updated"
                : $"Load #{loadId} — status: {newStatus}";
            var body = $"<p>Load <strong>#{loadId}</strong> was updated" +
                       (string.IsNullOrEmpty(newStatus) ? "." : $": <strong>{System.Web.HttpUtility.HtmlEncode(newStatus)}</strong>") + "</p>" +
                       (string.IsNullOrEmpty(messageHtml) ? "" : messageHtml) +
                       (string.IsNullOrEmpty(updatedBy) ? "" : $"<p><small>Updated by: {System.Web.HttpUtility.HtmlEncode(updatedBy)}</small></p>");

            try
            {
                if (!string.IsNullOrEmpty(shipperUserId))
                    SendToUserId(shipperUserId, subj, body);
            }
            catch { /* */ }
            try
            {
                if (!string.IsNullOrEmpty(assignedCarrierUserId))
                    SendToUserId(assignedCarrierUserId, subj, body);
            }
            catch { /* */ }
        }

        public static void NotifyLoadDataEdited(int loadId, ApplicationUser editor, string shipperUserId, string assignedCarrierUserId)
        {
            var eid = editor?.Id;
            const string s = "Load {0} — details changed";
            var subj = string.Format(s, loadId);
            var body = $"<p>Load <strong>#{loadId}</strong> was modified in the system.</p>" +
                       $"<p>Review the load in the app for current information.</p>";
            try
            {
                if (!string.IsNullOrEmpty(shipperUserId) && !string.Equals(shipperUserId, eid, StringComparison.Ordinal))
                    SendToUserId(shipperUserId, subj, body);
            }
            catch { /* */ }
            try
            {
                if (!string.IsNullOrEmpty(assignedCarrierUserId) && !string.Equals(assignedCarrierUserId, eid, StringComparison.Ordinal))
                    SendToUserId(assignedCarrierUserId, subj, body);
            }
            catch { /* */ }
        }

        static void SendToAllAdmins(string subject, string bodyHtml)
        {
            if (!TrySmtp()) return;
            var um = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var emails = new List<string>();
            foreach (var u in um.Users.ToList())
            {
                if (string.IsNullOrEmpty(u.Email)) continue;
                if (um.IsInRole(u.Id, LoadboardPermissions.RoleAdmin))
                    emails.Add(u.Email);
            }
            var from = ConfigurationManager.AppSettings["SmtpFrom"] ?? "noreply@local";
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var port = 587;
            if (int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var p)) port = p;
            var useSsl = ConfigurationManager.AppSettings["SmtpUseSsl"] != "false";
            var smtpUser = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPassword"];
            var client = new SmtpClient(host, port) { EnableSsl = useSsl, DeliveryMethod = SmtpDeliveryMethod.Network, UseDefaultCredentials = false };
            if (!string.IsNullOrEmpty(smtpUser)) client.Credentials = new NetworkCredential(smtpUser, pass);
            foreach (var to in emails.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var msg = new MailMessage(from, to) { IsBodyHtml = true, Body = bodyHtml, Subject = subject };
                client.Send(msg);
            }
        }

        static void SendToUserId(string userId, string subject, string bodyHtml)
        {
            if (string.IsNullOrEmpty(userId) || !TrySmtp()) return;
            var um = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var user = um.FindById(userId);
            if (user == null || string.IsNullOrEmpty(user.Email)) return;
            var from = ConfigurationManager.AppSettings["SmtpFrom"] ?? "noreply@local";
            var host = ConfigurationManager.AppSettings["SmtpHost"];
            var port = 587;
            if (int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var p)) port = p;
            var useSsl = ConfigurationManager.AppSettings["SmtpUseSsl"] != "false";
            var su = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPassword"];
            var client = new SmtpClient(host, port) { EnableSsl = useSsl, DeliveryMethod = SmtpDeliveryMethod.Network, UseDefaultCredentials = false };
            if (!string.IsNullOrEmpty(su)) client.Credentials = new NetworkCredential(su, pass);
            var msg = new MailMessage(from, user.Email) { IsBodyHtml = true, Body = bodyHtml, Subject = subject };
            client.Send(msg);
        }

        static bool TrySmtp() => !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["SmtpHost"]);
    }
}
