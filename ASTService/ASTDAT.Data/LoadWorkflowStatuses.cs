namespace ASTDAT.Data.Models
{
    /// <summary>Load lifecycle: draft → posted → claimed → assigned → in_transit → delivered → completed; cancelled from several states.</summary>
    public static class LoadWorkflowStatuses
    {
        public const string Draft = "draft";
        public const string Posted = "posted";
        public const string Claimed = "claimed";
        public const string Assigned = "assigned";
        public const string InTransit = "in_transit";
        public const string Delivered = "delivered";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";
    }
}
