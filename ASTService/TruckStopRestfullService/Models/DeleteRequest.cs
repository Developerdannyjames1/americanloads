using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckStopRestfullService.Models
{
    public class DeleteRequest : TsRequest
    {
        public int Reason { get; set; }
        public string Comment { get; set; }
    }
}
