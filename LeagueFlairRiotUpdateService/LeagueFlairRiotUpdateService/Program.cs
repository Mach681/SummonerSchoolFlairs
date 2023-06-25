using Azure.Messaging.ServiceBus;
using LeagueFlairRiotUpdateService;
using LeagueFlairRiotUpdateService.Handlers;
using LeagueFlairRiotUpdateService.Helpers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
.ConfigureAppConfiguration((_, config) =>
{
    // Pull in settings from local.settings.json
    // local.settings.json should be .gitignored
    config
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddJsonFile("local.appsettings.json", optional: true, reloadOnChange: true)
        .Build();
})
.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton(_provider =>
    {
        string sbcstr = hostContext.Configuration.GetValue<string>("AzureServiceBusConnectionString");
        Console.WriteLine($"AzureServiceBusConnectionString {sbcstr[..40]}****");
        return new ServiceBusClient(sbcstr);
    });

    // TODO: Add required dependencies here.
    services.AddSingleton<StorageHelper>();
    //services.AddSingleton(new TelemetryClient(new TelemetryConfiguration(hostContext.Configuration.GetValue<string>("ApplicationInsights:InstrumentationKey"))));

    services.AddSingleton<IEventHandler, RiotUpdateHandler>();

    services.AddHostedService<ListenerService>();
})
.UseConsoleLifetime()
.Build()
.Run();