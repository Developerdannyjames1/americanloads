using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class UploadLogModel
    {
        public int Id { get; set; }
        public DateTime Begin { get; set; }
        public string Company { get; set; }
        public string LoadNo { get; set; }
        public bool? Converted { get; set; }
        public string ConvertError { get; set; }
        public bool? Uploaded { get; set; }
        public string UploadError { get; set; }
        public DateTime? Finished { get; set; }
        public string MessageID { get; set; }
        public string FileName { get; set; }
        public int? SendAttempts { get; set; }
        public string EmailBody { get; set; }
    }
}
