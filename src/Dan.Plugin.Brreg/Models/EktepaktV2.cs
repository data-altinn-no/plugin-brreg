
using System;

namespace Dan.Plugin.Brreg.Models.EktepaktV2
{
    public class EktepaktV2
    {
        public string rolleinnehaver { get; set; }
        public int antallEktepakt { get; set; }
        public string oppslagstidspunkt { get; set; }
        public string spraak { get; set; }
        public Ektepakt[] ektepakt { get; set; }
    }

    public class Ektepakt
    {
        public int ektepaktnummer { get; set; }
        public DateTime innkomsttidspunkt { get; set; }
        public string ektepakttype { get; set; }
        public string ektepakttypebeskrivelse { get; set; }
        public string status { get; set; }
        public string statusbeskrivelse { get; set; }
        public string opprettelse { get; set; }
        public string opprettelsestypebeskrivelse { get; set; }
        public bool harSupplerendeOpplysningerIDokument { get; set; }
        public object[] paategning { get; set; }
        public Avtaleinnhold[] avtaleinnhold { get; set; }
        public Rolle[] rolle { get; set; }
    }

    public class Avtaleinnhold
    {
        public string avtaletype { get; set; }
        public string avtaletypebeskrivelse { get; set; }
    }

    public class Rolle
    {
        public string rolletype { get; set; }
        public string rolletypebeskrivelse { get; set; }
        public Person person { get; set; }
        public Avtaleinnhold1[] avtaleinnhold { get; set; }
    }

    public class Person
    {
        public Navn navn { get; set; }
        public Adresse adresse { get; set; }
    }

    public class Navn
    {
        public string fornavn { get; set; }
        public object mellomnavn { get; set; }
        public string etternavn { get; set; }
    }

    public class Adresse
    {
        public string adresseType { get; set; }
        public string brukskategori { get; set; }
        public object coNavn { get; set; }
        public object adressenavnTillegg { get; set; }
        public string adressenavn { get; set; }
        public object kortAdressenavn { get; set; }
        public Nummer nummer { get; set; }
        public string bruksenhetsnummer { get; set; }
        public object vegadresseID { get; set; }
        public Poststed poststed { get; set; }
        public Kommune kommune { get; set; }
    }

    public class Nummer
    {
        public string nummer { get; set; }
        public object bokstav { get; set; }
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

    public class Avtaleinnhold1
    {
        public string avtaletype { get; set; }
        public string avtaletypebeskrivelse { get; set; }
    }
}
