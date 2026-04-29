namespace ASTDAT.Web.Infrastructure
{
    public static class OnboardingStatuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Suspended = "suspended";
        public const string NeedsReview = "needs_review";

        public static readonly string[] All =
        {
            Pending, Approved, Rejected, Suspended, NeedsReview
        };

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var t = value.Trim();
            if (t.Equals("Pending", System.StringComparison.OrdinalIgnoreCase) || t.Equals("pending", System.StringComparison.Ordinal))
                return Pending;
            if (t.Equals("Approved", System.StringComparison.OrdinalIgnoreCase)) return Approved;
            if (t.Equals("Rejected", System.StringComparison.OrdinalIgnoreCase)) return Rejected;
            if (t.Equals("Suspended", System.StringComparison.OrdinalIgnoreCase)) return Suspended;
            if (t.Equals("needs_review", System.StringComparison.OrdinalIgnoreCase) || t.Equals("NeedsReview", System.StringComparison.OrdinalIgnoreCase))
                return NeedsReview;
            if (t.Equals("approved", System.StringComparison.Ordinal) || t.Equals("rejected", System.StringComparison.Ordinal) ||
                t.Equals("suspended", System.StringComparison.Ordinal))
            {
                return t.ToLowerInvariant();
            }
            return t.ToLowerInvariant();
        }

        public static bool IsApprovedState(string s)
        {
            var n = Normalize(s);
            return n == Approved;
        }
    }
}
