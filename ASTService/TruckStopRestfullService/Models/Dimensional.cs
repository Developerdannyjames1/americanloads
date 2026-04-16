using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TruckStopRestfullService.Models
{
    public class Dimensional
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Length { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Width { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Weight { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Height { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Cube { get; set; }
        public int PalletCount { get; set; } = 0;
        public int PieceCount { get; set; } = 0;
    }
}
