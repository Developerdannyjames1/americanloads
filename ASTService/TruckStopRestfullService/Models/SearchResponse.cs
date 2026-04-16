using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TruckStopRestfullService.Models
{
    public class SearchResponse
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }
        [JsonProperty("data")]
        public List<Load> Loads { get; set; }

    }
}
