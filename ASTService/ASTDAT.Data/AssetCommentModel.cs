using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data.Models
{
    public class AssetCommentModel
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public virtual AssetModel Asset { get; set; }
        public string Comment { get; set; }
    }
}
