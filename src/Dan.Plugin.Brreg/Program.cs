using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Dan.Common.Extensions;
using Dan.Plugin.Brreg.Config;
using Dan.Plugin.Skatteetaten.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

// Load the Maskinporten certificate before the host is built. ConfigureDanPluginDefaults loads environment variables,
// which includes local.settings.json when developing locally and the application settings in Azure, so the same values
// are read here directly. The certificate used to be fetched lazily from a Settings property getter, but the options
// binder reads every property synchronously and a blocking Key Vault call there deadlocks worker startup.
var certificate = Environment.GetEnvironmentVariable("Certificate");
if (string.IsNullOrEmpty(certificate))
{
    var vaultName = Environment.GetEnvironmentVariable("VaultName");
    var certName = Environment.GetEnvironmentVariable("CertName");
    certificate = await new PluginKeyVault(vaultName).GetCertificateAsBase64(certName);
}

var host = new HostBuilder()
    .ConfigureDanPluginDefaults()
    .ConfigureAppConfiguration((context, configuration) =>
    {
        // Add more configuration sources if necessary. ConfigureDanPluginDefaults will load environment variables, which includes
        // local.settings.json (if developing locally) and applications settings for the Azure Function
    })
    .ConfigureServices((context, services) =>
    {
        // Add any additional services here

        // This makes IOption<Settings> available in the DI container.
        services.Configure<Settings>(context.Configuration);
        services.PostConfigure<Settings>(settings => settings.Certificate = certificate);

        var maskinportenSettings = new MaskinportenSettings()
        {
            EncodedX509 = certificate,
            ClientId = Environment.GetEnvironmentVariable("MP:ClientId"),
            Scope = Environment.GetEnvironmentVariable("MP:Scope"),
            Environment = Environment.GetEnvironmentVariable("MP:Environment")
        };

        services.AddMaskinportenHttpClient<SettingsX509ClientDefinition>("myMaskinportenClient", maskinportenSettings);
    })
    .Build();

await host.RunAsync();
