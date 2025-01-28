using Dan.Plugin.Skatteetaten.Config;
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
        private string EncodedX509Cert { get; set; }

        public string Certificate
        {
            get
            {
                return EncodedX509Cert ?? new PluginKeyVault(VaultName).GetCertificateAsBase64(CertName).Result;
            }
            set
            {
                EncodedX509Cert = value;
            }
        }
    }
}
