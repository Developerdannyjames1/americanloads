using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class LoadTypeModel
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(200)]
        public string IdDAT { get; set; }

        [StringLength(200)]
        public string NameDAT { get; set; }

        [StringLength(200)]
        public string IdTS { get; set; }

        [StringLength(200)]
        public string NameTS { get; set; }
        public int? TsId { get; set; }
    }
}
