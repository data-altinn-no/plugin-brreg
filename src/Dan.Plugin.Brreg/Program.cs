using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Dan.Common.Extensions;
using Dan.Plugin.Brreg.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

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
        var configurationRoot = context.Configuration;
        services.Configure<Settings>(configurationRoot);
        var applicationSettings = services.BuildServiceProvider().GetRequiredService<IOptions<Settings>>().Value;

        var maskinportenSettings = new MaskinportenSettings()
        {
            EncodedX509 = applicationSettings.Certificate,
            ClientId = Environment.GetEnvironmentVariable("MP:ClientId"),
            Scope = Environment.GetEnvironmentVariable("MP:Scope"),
            Environment = Environment.GetEnvironmentVariable("MP:Environment")
        };

        services.AddMaskinportenHttpClient<SettingsX509ClientDefinition>("myMaskinportenClient", maskinportenSettings);
    })



    .Build();

await host.RunAsync();
