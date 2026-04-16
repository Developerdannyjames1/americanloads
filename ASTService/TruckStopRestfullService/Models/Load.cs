using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TruckStopRestfullService.Models
{
    public class Load : TsRequest
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LoadId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? LegacyLoadId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string PostAsUserId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public User PostAsUser { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CreatedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string UpdatedBy { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? CreateDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? UpdateDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LoadState LoadState { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LoadStateReason LoadStateReason { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? TenderId { get; set; }
        [JsonIgnore]
        public bool TenderIdSpecified { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CarrierName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<LoadActivity> LoadActivity { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public EquipmentAttributes EquipmentAttributes { get; set; }
        public int? CommodityId { get; set; }
        [JsonIgnore]
        public bool CommodityIdSpecified { get; set; }
        public List<LoadStop> LoadStops { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? FreightClassId { get; set; }
        [JsonIgnore]
        public bool FreightClassIdSpecified { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LoadNumber { get; set; }
        public bool LoadTrackingRequired { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RateAttributes RateAttributes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<KeyValuePair<string, string>> CustomData { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dimensional Dimensional { get; set; }
        public LoadActionAttributes LoadActionAttributes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string LoadLabel { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string TenderNotes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> LoadReferenceNumbers { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TermsAndConditions TermsAndConditions { get; set; }

        [JsonIgnore]
        public string Text {
            get
            {
                return $"{LoadId}" + (LegacyLoadId.HasValue ? $"(LegacyId {LegacyLoadId})" : "");
            }
        }
    }
}
