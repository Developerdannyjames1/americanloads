using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TruckStopRestfullService.Models
{
    public class LoadStop
    {
        public string Id { get; set; }
        public int Type { get; set; }
        public int Sequence { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //[JsonConverter(typeof(DateConverter))] 
        public DateTime? EarlyDateTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //[JsonConverter(typeof(DateConverter))] 
        public DateTime? LateDateTime { get; set; }
        public Location Location { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string StopNotes { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ReferenceNumbers { get; set; }

    }
}
