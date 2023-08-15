using System;
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
    }
}
