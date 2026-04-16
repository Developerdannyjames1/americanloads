using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data
{
    public class ImportLogModel
    {
        [Key]
        public int Id { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime FileDateTime { get; set; }
        public DateTime Created { get; set; }
    }
}
