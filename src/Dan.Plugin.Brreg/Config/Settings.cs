using System;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
// using System.Configuration;

namespace Dan.Plugin.Brreg.Config
{
    /// <summary>
    /// Application settings
    /// </summary>
    public class Settings 
    {
        public string FrivilligUri { get; set; }

        public string TilskuddsregisterUrl { get; set; }

        public string ES_BR_ProductsUserName { get; set; }

        public string ES_BR_ProductsPassword { get; set; }

        public string BR_endpoint_address { get; set; }

        public string EktepaktPassword { get; set; }

        public string EktepaktUserName { get; set; }

        public string EktepaktUri { get; set; }

        public string RegnskapsregisteretUri { get; set; }
        public string RegnskapsregisteretPw { get; set; }
        public string RegnskapsregisteretUsername { get; set; }

        public string AnnouncementUrl { get; set; }
        public string StotteregisterUrl { get; set; }
        public string VaultName { get; set; }

        public string CertName { get; set; }

        public string LosoreURI { get; set; }

        public string EktepaktV2Uri { get; set; }

        /// <summary>
        /// Base64-encoded Maskinporten certificate. Set from the Certificate setting when present, otherwise loaded
        /// from Key Vault once at startup in Program.cs. It must not be fetched lazily here: the options binder reads
        /// every property synchronously, and a blocking Key Vault call there deadlocks worker startup.
        /// </summary>
        public string Certificate { get; set; }
    }
}
