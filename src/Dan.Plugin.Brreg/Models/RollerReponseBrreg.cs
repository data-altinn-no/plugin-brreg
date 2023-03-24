using System;
using Newtonsoft.Json;

namespace Dan.Plugin.Brreg.Models.Roller
{

    public class RollerResponseBrreg
    {
        public Rollegrupper[] rollegrupper { get; set; }
    }

    public class Rollegrupper
    {
        public Type type { get; set; }
        public string sistEndret { get; set; }
        public Roller[] roller { get; set; }
    }

    public class Type
    {
        public string kode { get; set; }
        public string beskrivelse { get; set; }
    }

    public class Roller
    {
        public Type type { get; set; }
        public Person person { get; set; }
        public bool fratraadt { get; set; }
        public int rekkefolge { get; set; }
        public Enhet enhet { get; set; }
    }

    public class Person
    {
        [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
        public DateTime? fodselsdato { get; set; }
        public Navn navn { get; set; }
        public bool erDoed { get; set; }
    }

    public class Navn
    {
        public string fornavn { get; set; }
        public string mellomnavn { get; set; }
        public string etternavn { get; set; }
    }

    public class Enhet
    {
        public string organisasjonsnummer { get; set; }
        public Organisasjonsform organisasjonsform { get; set; }
        public string[] navn { get; set; }
        public bool erSlettet { get; set; }
    }

    public class Organisasjonsform
    {
        public string kode { get; set; }
        public string beskrivelse { get; set; }
    }
}
