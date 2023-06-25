using Azure.Messaging.ServiceBus;
using LeagueFlairUpdateJob.Classes;
using LeagueFlairUpdateJob.Helpers;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

ConfigurationBuilder builder = new ConfigurationBuilder();
IConfiguration config = builder
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddJsonFile("local.appsettings.json", optional: true, reloadOnChange: true)
    .Build();

ServiceBusClient sbClient = new(config.GetValue<string>("AzureServiceBusConnectionString"));

StorageHelper storage = new(config, sbClient);

// Delete old SummonerInfo records (365 days)
List<SummonerInfo> oldSummonerInfos = await storage.QueryCloudTable<SummonerInfo>("SummonerInfo", x => x.Signup_Timestamp < DateTime.UtcNow.AddYears(-1));

foreach (SummonerInfo info in oldSummonerInfos)
{
    await storage.DeleteCloudTable("SummonerInfo", info);
}

// Delete old RedditFlairUser entries (2 days)
List<RedditFlairUser> oldRedditFlairUsers = await storage.QueryCloudTable<RedditFlairUser>("RedditFlairUser", x => x.ExpiresOn < DateTime.UtcNow);

foreach (RedditFlairUser user in oldRedditFlairUsers)
{
    await storage.DeleteCloudTable("RedditFlairUser", user);
}

// Update any users who haven't been updated in 8 hours or more
List<SummonerInfo> updateSummonerInfos = await storage.QueryCloudTable<SummonerInfo>("SummonerInfo", x => (x.Last_Updated) < DateTime.UtcNow.AddHours(-8));

foreach (SummonerInfo info in updateSummonerInfos)
{
    ServiceBusMessage message = new ServiceBusMessage()
    {
        ContentType = "application/json",
        CorrelationId = Guid.NewGuid().ToString(),
        ReplyTo = "OauthCallback",
        Subject = "SummonerRegistered",
        Body = new BinaryData(JsonSerializer.Serialize(info))
    };

    await storage.SendServiceBusMessage(message);
}