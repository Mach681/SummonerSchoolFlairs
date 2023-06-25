using LeagueFlairRedditSignup.Helpers;
using LeagueFlairRedditSignup.HostedService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reddit;

// C# initial setup and dependency injection

// A "host" will be a long running process.
// This is useful for daemon/service processes.
// Container orchestrators such as Kubernetes can send the kill switch to any hosted service
//   and it will run a Shutdown routine before ending, thus allowing for cleanup.
IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
.ConfigureAppConfiguration((_, config) =>
{
    // Microsoft config extensions allow for adding values from config files, environment variables, etc.
    //   in a way that the newest one wins.
    // Our hierarchy is to use default (non-sensitive) secrets in an appsettings.json file
    //   then take sensitive settings from environment variables, which is the pattern in containerized applications,
    //   and then finally take a local.appsettings.json file, which is developer secrets for local development.
    // Note: It is critical to NOT check-in the local.appsettings.json file.  It's been added to the .gitignore and .dockerignore files.

    // Create the config.  See below for examples of how to use the values in it.
    config
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddJsonFile("local.appsettings.json", optional: true, reloadOnChange: true)
        .Build();
})
.ConfigureServices((hostContext, services) =>
{
    // Register services and classes used by this system.
    // Dependency injection will automatically call the correct constructor using services defined here.

    // Create the reddit client for use interacting with reddit
    services.AddSingleton(x =>
    {
        // The reddit client requires an AppId, RefreshToken and AppSecret.  Create a local.appsettings.json file from the example and add the correct values.
        // These values are being pulled from the config listed above.
        RedditClient reddit = new RedditClient(appId: hostContext.Configuration.GetValue<string>("RedditAppId"),
            refreshToken: hostContext.Configuration.GetValue<string>("RedditRefreshToken"), appSecret: hostContext.Configuration.GetValue<string>("RedditAppSecret"));
        return reddit;
    });

    // Quick helper class to save things to Azure Table Storage
    // Relies on a variable called AZURE_STORAGE_ACCOUNT, which is the connection string to an Azure storage account where we'll hold summoner data.
    // I should move that out to here so it's passed in as a value so ALL config is done here, but I haven't cleaned it all up yet.
    services.AddSingleton<StorageHelper>();

    // This is the Hosted Service (the long running process).
    // See the RedditHandler.cs file for how it works.
    // In short, it has a StartAsync (that happens on start) and a StopAsync (that happens on shutdown)
    // Internally it wires up a listener for new Reddit events and will handle those events.
    // That section is reallyt he only business logic.  The rest of this is boilerplate and setting up the environment.
    services.AddHostedService<RedditHandler>();    
});

// Create the host.
IHost host = hostBuilder.Build();

// Call RunAsync, which will call StartAsync on all hosted services (we only have the one).
// When the host is told to stop (through a sigterm or kill command, for example),
//   it'll call StopAsync on all hosted services, wait for them to end, and then terminate the program.
// This is how we manage the lifecycle of the process/container.
await host.RunAsync();
