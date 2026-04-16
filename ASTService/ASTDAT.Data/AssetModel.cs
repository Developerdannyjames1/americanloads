using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data.Models
{
    public class AssetModel
    {
        public int Id { get; set; }
        public string PostersReferenceId { get; set; }
        public bool? Ltl { get; set; }
        public int? Count { get; set; }
        public int? Stops { get; set; }
        public bool? IncludeAsset { get; set; }
        public bool? PostToExtendedNetwork { get; set; }
        public DateTime? AvailabilityEarliest { get; set; }
        public DateTime? AvailabilityLatest { get; set; }
        public string AssetId { get; set; }
        public int? DimensionsLengthFeet { get; set; }
        public int? DimensionsWeightPounds { get; set; }
        public int? DimensionsHeightInches { get; set; }
        public int? DimensionsVolumeCubic { get; set; }
        public int? DestinationId { get; set; }
        public virtual OriginDestinationModel Destination { get; set; }
        public int? OriginId { get; set; }
        public virtual OriginDestinationModel Origin { get; set; }
        [StringLength(20)]
        public string EquipmentType { get; set; }
        public Int16 RateEateBasedOn { get; set; }
        public decimal RateBaseRateDollars { get; set; }
        public int? RateRateMiles { get; set; }
        [StringLength(50)]
        public string TruckStopsEnhancements { get; set; }
        [StringLength(20)]
        public string TruckStopsPosterDisplayName { get; set; }
        //public DateTime Created { get; set; }
        //public DateTime Refreshed { get; set; }
        //public int State { get; set; }
    }
}
