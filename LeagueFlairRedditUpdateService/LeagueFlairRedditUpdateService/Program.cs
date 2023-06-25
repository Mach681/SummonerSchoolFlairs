using Azure.Messaging.ServiceBus;
using LeagueFlairRedditUpdateService;
using LeagueFlairRedditUpdateService.Handlers;
using LeagueFlairRedditUpdateService.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reddit;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
.ConfigureAppConfiguration((_, config) =>
{
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

    services.AddSingleton(x =>
    {
        RedditClient reddit = new RedditClient(appId: hostContext.Configuration.GetValue<string>("RedditAppId"),
            refreshToken: hostContext.Configuration.GetValue<string>("RedditRefreshToken"), appSecret: hostContext.Configuration.GetValue<string>("RedditAppSecret"));
        return reddit;
    });

    services.AddSingleton<StorageHelper>();

    services.AddSingleton<IEventHandler, RedditUpdateHandler>();

    services.AddHostedService<ListenerService>();
});

IHost host = hostBuilder.Build();

await host.RunAsync();