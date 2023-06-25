using Azure.Messaging.ServiceBus;
using LeagueFlairRedditUpdateService.Classes;
using LeagueFlairRedditUpdateService.Helpers;
using Microsoft.Extensions.Configuration;
using Reddit;
using Reddit.Things;
using System.Text.Json;
using Subreddit = Reddit.Controllers.Subreddit;

namespace LeagueFlairRedditUpdateService.Handlers
{
    public class RedditUpdateHandler : IEventHandler
    {
        private readonly StorageHelper _storageHelper;
        private readonly Subreddit _subreddit;

        public RedditUpdateHandler(StorageHelper storageHelper, IConfiguration config, RedditClient reddit)
        {
            _storageHelper = storageHelper;
            _subreddit = reddit.Subreddit(config.GetValue<string>("FlairSubreddit"));
            
        }

        public string HandlerName => "RedditUpdateHandler";

        public bool CanHandle(ServiceBusReceivedMessage message)
        {
            return string.Equals(message.Subject, "SummonerRankUpdated") && string.Equals(message.ReplyTo, "RiotUpdateHandler");
        }

        public async Task Handle(ServiceBusReceivedMessage message)
        {
            SummonerInfo info = JsonSerializer.Deserialize<SummonerInfo>(message.Body);

            // Get the current flair
            try
            {
                List<FlairListResult> userFlairs = _subreddit.Flairs.GetFlairList(username: info.Reddit_Username);

                if (userFlairs.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(userFlairs[0].FlairText) && userFlairs[0].FlairText.Equals(info.League_Rank))
                    {
                        // Flairs already match.  Time to bail.
                        return;
                    }
                }

                await _subreddit.Flairs.CreateUserFlairAsync(info.Reddit_Username, info.League_Rank);
                info.Last_Updated = DateTime.UtcNow;
                await _storageHelper.UpsertCloudTable("SummonerInfo", info);
            }
            // This means the reddit user has been deleted.  There's nothing to flair, so delete the record and move on.
            catch (Reddit.Exceptions.RedditForbiddenException)
            {
                await _storageHelper.DeleteCloudTable<SummonerInfo>("SummonerInfo", info);
            }
        }
    }
}
