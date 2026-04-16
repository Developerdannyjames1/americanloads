using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TruckStopRestfullService.Models
{
    public class TsResponse
    {
        public bool Success { get; set; }
        public object Data { get; set; }
        public List<StatusSet> StatusSet { get; set; }
    }

    public class StatusSet
    {
        public int Code { get; set; }
        public string Descriptor { get; set; }
        public string Message { get; set; }
        public string Field { get; set; }

    }
}
