using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data.Models
{
    public class OriginDestinationModel
    {
        public int Id { get; set; }
        public Int16 Type { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public int StateId { get; set; }
        public virtual StateModel State { get; set; }
        [StringLength(50)]
        public string PostalCode { get; set; }
        [StringLength(100)]
        public string Country { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
