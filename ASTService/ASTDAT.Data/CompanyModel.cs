using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class CompanyModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
