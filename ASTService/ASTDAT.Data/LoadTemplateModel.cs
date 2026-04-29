using System.ComponentModel.DataAnnotations;

namespace ASTDAT.Data.Models
{
    public class LoadTemplateModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; }

        /// <summary>Admin-owned global template.</summary>
        public bool IsGlobal { get; set; }

        /// <summary>Company-owned template for shippers/dispatchers.</summary>
        public int? CompanyId { get; set; }

        public int? LoadTypeId { get; set; }
        public int? AssetLength { get; set; }
        public int? Weight { get; set; }
        public int? OriginId { get; set; }
        public int? DestinationId { get; set; }
        [StringLength(200)]
        public string OriginCity { get; set; }
        [StringLength(10)]
        public string OriginState { get; set; }
        [StringLength(200)]
        public string DestinationCity { get; set; }
        [StringLength(10)]
        public string DestinationState { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        [StringLength(128)]
        public string CreatedByUserId { get; set; }
    }
}
