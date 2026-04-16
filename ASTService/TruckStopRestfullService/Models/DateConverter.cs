using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;

namespace TruckStopRestfullService.Models
{
    public class DateConverter : IsoDateTimeConverter
    {
        public DateConverter()
        {
            //DateTimeFormat = "yyyy'-'MM'-'ddTHH':'mm':'ss";
            DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        }
    }
}
