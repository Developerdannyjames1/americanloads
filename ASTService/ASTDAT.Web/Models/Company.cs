using System;
using System.ComponentModel.DataAnnotations;

namespace ASTDAT.Web.Models
{
    /// <summary>Shipper or carrier organization; onboarding status applies to all users in the company.</summary>
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(256)]
        public string Name { get; set; }

        /// <summary>Shipper or Carrier</summary>
        [Required]
        [StringLength(32)]
        public string CompanyType { get; set; }

        /// <summary>pending, approved, rejected, suspended, needs_review (lowercase in API)</summary>
        [StringLength(32)]
        public string OnboardingStatus { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}
