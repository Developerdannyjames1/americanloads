using System;
using System.ComponentModel.DataAnnotations;

namespace ASTDAT.Data.Models
{
    public class LoadClaimModel
    {
        public int Id { get; set; }

        public int LoadId { get; set; }

        [StringLength(128)]
        public string CarrierUserId { get; set; }

        /// <summary>claim or bid</summary>
        [StringLength(16)]
        public string ClaimType { get; set; }

        public decimal? BidAmount { get; set; }

        [StringLength(2000)]
        public string Message { get; set; }

        /// <summary>pending, accepted, rejected</summary>
        [StringLength(32)]
        public string Status { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime? ResolvedUtc { get; set; }

        [StringLength(128)]
        public string ResolvedByUserId { get; set; }
    }
}
