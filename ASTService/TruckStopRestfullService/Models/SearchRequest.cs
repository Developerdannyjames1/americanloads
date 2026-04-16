using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TruckStopRestfullService.Models
{
    public class SearchRequest : TsRequest
    {
        [JsonProperty("pagination", NullValueHandling = NullValueHandling.Ignore)]
        public Pagination Pagination { get; set; } = null;

        [JsonProperty("searchCriteria", Required = Required.Always)]
        public List<SearchCriteria> SearchCriteria { get; set; }

        [JsonProperty("sortCriteria", NullValueHandling = NullValueHandling.Ignore)]
        public List<SortCriteria> SortCriteria { get; set; } = null;

        public SearchRequest()
        {
            SearchCriteria = new List<SearchCriteria>();
        }
    }

    public class Pagination
    {
        [JsonProperty("pageNumber")] 
        public int PageNumber { get; set; }
        [JsonProperty("pageSize")] 
        public int PageSize { get; set; }

        [JsonProperty("totalItemCount")] 
        public int TotalItemCount { get; set; }
        [JsonIgnore]
        public bool TotalItemCountSpecified { get; set; } = false;

        [JsonProperty("totalPages")] 
        public int TotalPages { get; set; }
        [JsonIgnore]
        public bool TotalPagesSpecified { get; set; } = false;

        [JsonProperty("resultsTruncated")] 
        public bool ResultsTruncated { get; set; }
        [JsonIgnore]
        public bool ResultsTruncatedSpecified { get; set; } = false;
    }

    public class SearchCriteria
    {
        [JsonProperty("name")] 
        public string Name { get; set; }
        [JsonProperty("operator")] 
        public string Operator { get; set; }
        [JsonProperty("value")] 
        public string Value { get; set; }
        [JsonProperty("valueTo", NullValueHandling = NullValueHandling.Ignore)] 
        public string ValueTo { get; set; }
        [JsonProperty("logicalOperator", NullValueHandling = NullValueHandling.Ignore)] 
        public string LogicalOperator { get; set; }

    }

    public class SortCriteria
    {
        [JsonProperty("name")] 
        public string Name { get; set; }
        [JsonProperty("direction", NullValueHandling = NullValueHandling.Ignore)] 
        public string Direction { get; set; }
    }
}
