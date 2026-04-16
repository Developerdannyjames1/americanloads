using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckStopRestfullService.Models
{
    public class LoadState
    {
        public string LoadStateDescription { get; set; }
        public LoadStates loadStateId { get; set; }
    }
    public class LoadStateReason
    {
        public string LoadStateReasonDescription { get; set; }
        public LoadStateReasons LoadStateReasonId { get; set; }
    }
    public class LoadActivity
    {
        public string Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public LoadState LoadState { get; set; }
        public LoadStateReason LoadStateReason { get; set; }
        public string Comment { get; set; }
    }
    public class TermsAndConditions
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class LoadActionAttributes
    {
        public string LoadActionId { get; set; }
        public string LoadActionOption { get; set; }
    }
    public class EquipmentAttributes
    {
        public int? EquipmentTypeId { get; set; }
        public List<int> EquipmentOptions { get; set; }
        public int? TransportationModeId { get; set; }
        public string OtherEquipmentNeeds { get; set; }
    }
    public class RateAttributes
    {
        public Rate PostedAllInRate { get; set; }
        public Rate TenderAllInRate { get; set; }
    }
    public class Rate
    {
        public decimal? Amount { get; set; }
        public string CurrencyCode { get; set; }
    }
}
