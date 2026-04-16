using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data.Models
{
    public class LoadModel
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
        /// <summary>
        /// ID in DAT Service
        /// </summary>
        public string AssetId { get; set; }
        public int? DimensionsLengthFeet { get; set; }
        public int? DimensionsWeightPounds { get; set; }
        public int? DimensionsHeightInches { get; set; }
        public int? DimensionsVolumeCubic { get; set; }

        public int? DestinationId { get; set; }
        public virtual OriginDestinationModel Destination { get; set; }

        public string EquipmentType { get; set; }

        public int OriginId { get; set; }
        public virtual OriginDestinationModel Origin { get; set; }

        public decimal CarrierAmount { get; set; }
        public Int16 RateEateBasedOn { get; set; }
        public int? RateRateMiles { get; set; }
        public string TruckStopsEnhancements { get; set; }
        public string TruckStopsPosterDisplayName { get; set; }
        public string ClientName { get; set; }
        public string ClientLoadNum { get; set; }
        public string EmailID { get; set; }
        public DateTime? DateLoaded { get; set; }
        public DateTime? DateRefreshed { get; set; }
        public DateTime? DateDatLoaded { get; set; }
        public DateTime? DateDatRefreshed { get; set; }
        public DateTime? DateDatDeleted { get; set; }
        public DateTime? DateRTFLoaded { get; set; }
        public DateTime? DateTRTLoaded { get; set; }
        public DateTime? UntilDate { get; set; }
        public int? AssetLength { get; set; }

        public int? TrackStopId { get; set; }
        public DateTime? DateTSDeleted { get; set; }

        public string Comments { get; set; }

        public int? LoadTypeId { get; set; }
        public virtual LoadTypeModel LoadType { get; set; }
        public string Description { get; set; }
        public bool IsLoadFull { get; set; }
        public DateTime? PickUpDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string BanyanTechBOL { get; set; }
        public decimal? CustomerAmount { get; set; }
        public int? Weight { get; set; }

        public DateTime? CreateDate { get; set; }
        [StringLength(256)]
        public string CreatedBy { get; set; }
        [StringLength(15)]
        public string CreateLoc { get; set; }
        public DateTime? UpdateDate { get; set; }
        [StringLength(256)]
        public string UpdatedBy { get; set; }
        [StringLength(15)]
        public string UpdateLoc { get; set; }

        public List<LoadCommentModel> AllComments { get; set; }

        public string LengthWidthHeight { get; set; }
        public string UserNotes { get; set; }
        public bool? AllowUntilSat { get; set; }
		public bool? AllowUntilSun { get; set; }
		public Guid? TsLoadId { get; set; }
	}
}
