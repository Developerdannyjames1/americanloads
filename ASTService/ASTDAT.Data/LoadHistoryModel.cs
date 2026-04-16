using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class LoadHistoryModel
    {
        [Key]
        public int ID { get; set; }
        public int ProNumber { get; set; }
        [StringLength(50)]
        public string Status { get; set; }
        public string Office { get; set; }
        [StringLength(200)]
        public string Customer { get; set; }
        public decimal? Total { get; set; }
        public decimal? LHPay { get; set; }
        public decimal? TotalPay { get; set; }
        [StringLength(200)]
        public string PUName { get; set; }
        [StringLength(100)]
        public string PUCity { get; set; }
        [StringLength(2)]
        public string PUState { get; set; }
        [StringLength(200)]
        public string Consignee { get; set; }
        [StringLength(100)]
        public string ConsCity { get; set; }
        [StringLength(2)]
        public string ConsState { get; set; }
        public DateTime? ReadyDate { get; set; }
        [StringLength(10)]
        public string ReadyTime { get; set; }
        public DateTime? PUApptDate { get; set; }
        public string PUApptNote { get; set; }
        [StringLength(200)]
        public string Carrier { get; set; }
        [StringLength(200)]
        public string CarrRef { get; set; }
        [StringLength(50)]
        public string CarrPhone { get; set; }
        [StringLength(150)]
        public string CarrEmail { get; set; }
        [StringLength(150)]
        public string Dispatcher { get; set; }
        [StringLength(50)]
        public string DrvPhone { get; set; }
        [StringLength(150)]
        public string TruckNum { get; set; }
        [StringLength(150)]
        public string Trailer { get; set; }
        [StringLength(50)]
        public string CoveredBy { get; set; }
        [StringLength(50)]
        public string PaysBy { get; set; }
        public DateTime? WillPUDate { get; set; }
        [StringLength(10)]
        public string WillPUTime { get; set; }
        public DateTime? DispDate { get; set; }
        [StringLength(10)]
        public string DispTime { get; set; }
        public DateTime? LoadedDate { get; set; }
        [StringLength(10)]
        public string LoadedTime { get; set; }
        public int? Pieces { get; set; }
        public decimal? Weight { get; set; }
        public string Descript { get; set; }
        [StringLength(200)]
        public string LstChkCall { get; set; }
        public DateTime? ETADate { get; set; }
        [StringLength(10)]
        public string EtaTime { get; set; }
        public DateTime? ApptDate { get; set; }
        [StringLength(10)]
        public string ApptTime { get; set; }
        public string ApptNote { get; set; }
        public DateTime? DelivDate { get; set; }
        [StringLength(10)]
        public string DelivTime { get; set; }
        [StringLength(100)]
        public string SignedBy { get; set; }
        [StringLength(200)]
        public string CustRef { get; set; }
        [StringLength(10)]
        public string SalesRep { get; set; }
        public decimal? Miles { get; set; }
        public decimal? DHMiles { get; set; }
        public decimal? Charges { get; set; }
        public decimal? Expense { get; set; }
        public decimal? Profit { get; set; }
        public decimal? ProfitPerc { get; set; }
        public string HistoryNote { get; set; }
    }
}
