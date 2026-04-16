using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class TSLoginModel
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Message { get; set; }

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
