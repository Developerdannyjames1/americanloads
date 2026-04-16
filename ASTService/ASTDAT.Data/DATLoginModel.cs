using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class DATLoginModel
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Message { get; set; }

        public byte[] TokenPrimary { get; set; }
        public byte[] TokenSecondary { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
