using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dan.Plugin.Brreg.Models
{

    public class RollerResponse
    {
        public List<Rolle> Roller { get; set; }
    }

    public class Rolle
    {
        public string Kode { get; set; }

        public string Beskrivelse { get; set; }

        public string Navn { get; set; }

        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Fodselsdato { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Organisasjonsnummer { get; set; }
    }
}
