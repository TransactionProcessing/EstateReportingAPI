using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EstateReportingAPI.DataTrasferObjects
{
    public class CalendarYear
    {
        [JsonProperty("year")]
        public Int32 Year{ get; set; }
    }
}
