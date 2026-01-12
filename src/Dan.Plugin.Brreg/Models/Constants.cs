using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Brreg.Models
{
    public static class Constants
    {
        public const string SourceEnhetsregisteret = "Enhetsregisteret";
        public const int ERROR_ORGANIZATION_NOT_FOUND = 1;
        public static int ERROR_CCR_UPSTREAM_ERROR = 2;
        public static int ERROR_NO_REPORT_AVAILABLE = 3;
        public static int ERROR_ASYNC_REQUIRED_PARAMS_MISSING = 4;
        public static int ERROR_ASYNC_ALREADY_INITIALIZED = 5;
        public static int ERROR_ASYNC_NOT_INITIALIZED = 6;
        public static int ERROR_AYNC_STATE_STORAGE = 7;
        public static int ERROR_ASYNC_HARVEST_NOT_AVAILABLE = 8;
        public static int ERROR_CERTIFICATE_OF_REGISTRATION_NOT_AVAILABLE = 9;
        public static int ERROR_PRODUCT_LIST_NOT_AVAILABLE = 10;
        public static int ERROR_AUTHENTICATION = 11;
        public static int ERROR_PERSON_NOT_FOUND = 12;
        public static int ERROR_UNKNOWN = 13;
        public static int ERROR_PARAMETERS_MISSING = 14;
        public static string SourceLosoreregisteret = "Løsøreregisteret";
        public static string SourceRegnskapsregisteret = "Regnskapsregisteret";
        public const string EBEVIS = "eBevis";
        public const string SERIOSITET = "Seriøsitetsinformasjon";
        public const string EDUEDILIGENCE = "eDueDiligence";
        public const string DD = "OED";
        public const string DIGOKFRIV = "Digøk-friv";
        public const string ALTINNSTUDIOAPPS = "Altinn-Studio-Apps";

        public static string SourceStotteRegisteret = "Støtteregisteret";

        public const string SourceTilskuddsregisteret = "Tilskudd.no";

        public static int ERROR_CERTIFICATE_NOT_AVAILABLE = 15;

        public static string SourceFrivillighetsregisteret = "Frivillighetsregisteret";
    }
}
