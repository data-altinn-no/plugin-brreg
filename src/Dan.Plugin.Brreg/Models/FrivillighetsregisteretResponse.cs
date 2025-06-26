using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Brreg.Models
{


public class FrivillighetsregisteretResponse
{
    public string organisasjonsnummer { get; set; }
    public string frivilligOrganisasjonsstatus { get; set; }
    public string kontonummer { get; set; }
    public string innfoertDato { get; set; }
    public string foersteGangInnfoert { get; set; }
    public Grasrotandel grasrotandel { get; set; }
    public Regnskapsrapportering regnskapsrapportering { get; set; }
    public Vedtekter vedtekter { get; set; }
    public Icnpokategorier[] icnpoKategorier { get; set; }
    public Paategninger[] paategninger { get; set; }
    public _Links _links { get; set; }
}

public class Grasrotandel
{
    public bool deltarI { get; set; }
    public Utestengelsesperiode utestengelsesperiode { get; set; }
}

public class Utestengelsesperiode
{
    public string fraDato { get; set; }
    public string tilDato { get; set; }
}

public class Regnskapsrapportering
{
    public bool harPaatattSegRapporteringsplikt { get; set; }
    public string avslutningsdatoForRegnskapsperiode { get; set; }
    public Sistinnsendteaarsregnskap sistInnsendteAarsregnskap { get; set; }
}

public class Sistinnsendteaarsregnskap
{
    public int regnskapsaar { get; set; }
    public string registreringsdato { get; set; }
}

public class Vedtekter
{
    public bool frivilligRegistrerteVedtekter { get; set; }
    public string sistOppdaterteVedtekter { get; set; }
}

public class _Links
{
    public Property1 property1 { get; set; }
    public Property2 property2 { get; set; }
}

public class Property1
{
    public string href { get; set; }
}

public class Property2
{
    public string href { get; set; }
}

public class Icnpokategorier
{
    public string kategori { get; set; }
    public string icnpoNummer { get; set; }
    public string navn { get; set; }
    public int rekkefoelge { get; set; }
}

public class Paategninger
{
    public string identifikatorInformasjonstype { get; set; }
    public string paategning { get; set; }
}
}
