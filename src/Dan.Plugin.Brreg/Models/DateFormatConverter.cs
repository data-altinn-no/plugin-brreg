using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Converters;

namespace Dan.Plugin.Brreg.Models
{
    public class DateFormatConverter : IsoDateTimeConverter
    {
        public DateFormatConverter(string format)
        {
            DateTimeFormat = format;
        }
    }
}
