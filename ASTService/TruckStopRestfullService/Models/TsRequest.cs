using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TruckStopRestfullService.Models
{
    public class TsRequest
    {
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}
