using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data.Models
{
    public class LoadCommentModel
    {
        [Key]
        public int Id { get; set; }

        public int LoadId { get; set; }
        public LoadModel Load { get; set; }

        public string Comment { get; set; }

        [StringLength(128)]
        public string UserId { get; set; }

        public DateTime DateTime { get; set; }
    }
}
