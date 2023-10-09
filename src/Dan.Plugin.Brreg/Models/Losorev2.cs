using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Brreg.Models.LosoreV2
{

    public class LosoreV2
        {
            public string sokeparameter { get; set; }
            public int antallRettsstiftelser { get; set; }
            public DateTime oppslagstidspunkt { get; set; }
            public Rettsstiftelse[] rettsstiftelse { get; set; }
        }

        public class Rettsstiftelse
        {
            public string dokumentnummer { get; set; }
            public string type { get; set; }
            public string typeBeskrivelse { get; set; }
            public string status { get; set; }
            public string statusBeskrivelse { get; set; }
            public DateTime innkomsttidspunkt { get; set; }
            public object[] paategning { get; set; }
            public Rolle[] rolle { get; set; }
            public Formuesgode[] formuesgode { get; set; }
            public object[] prioritetsvikelse { get; set; }
            public Krav krav { get; set; }
        }

        public class Krav
        {
            public Belop[] belop { get; set; }
        }

        public class Belop
        {
            public double belop { get; set; }
            public string valuta { get; set; }
        }

        public class Rolle
        {
            public string rolletype { get; set; }
            public string rolletypeBeskrivelse { get; set; }
            public string rollegruppetype { get; set; }
            public string rollegruppetypeBeskrivelse { get; set; }
            public Rolleinnehaver rolleinnehaver { get; set; }
        }

        public class Rolleinnehaver
        {
            public string aktorType { get; set; }
            public string navn { get; set; }
            public string organisasjonsnummer { get; set; }
            public Adresse adresse { get; set; }
        }

        public class Adresse
        {
            public string adresseType { get; set; }
            public string brukskategori { get; set; }
            public string adresselinje1 { get; set; }
            public Poststed poststed { get; set; }
            public Kommune kommune { get; set; }
        }

        public class Poststed
        {
            public string navn { get; set; }
            public string postnummer { get; set; }
        }

        public class Kommune
        {
            public string kommunenummer { get; set; }
            public string kommunenavn { get; set; }
        }

        public class Formuesgode
        {
            public string identifiseringsmate { get; set; }
            public string type { get; set; }
            public string typeBeskrivelse { get; set; }
            public Eierandel eierandel { get; set; }
            public string avgrensingTingsinnbegrep { get; set; }
            public string avgrensingTingsinnbegrepBeskrivelse { get; set; }
        }

        public class Eierandel
        {
        }
}

