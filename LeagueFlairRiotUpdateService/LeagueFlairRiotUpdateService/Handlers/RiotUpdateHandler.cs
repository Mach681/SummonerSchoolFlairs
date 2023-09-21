using Azure.Core;
using Azure.Messaging.ServiceBus;
using LeagueFlairRiotUpdateService.Classes;
using LeagueFlairRiotUpdateService.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using MingweiSamuel.Camille.LeagueV4;
using MingweiSamuel.Camille.SummonerV4;
using RestSharp;
using System.Globalization;
using System.Text.Json;

namespace LeagueFlairRiotUpdateService.Handlers
{
    public class RiotUpdateHandler : IEventHandler
    {
        private static List<Region> regions = new List<Region> { new Region("na1", "na1"), new Region("euw1", "euw1"), new Region("kr", "kr"),
            new Region("br1", "br1"), new Region("eun1", "eun1"), new Region("jp1", "jp1"), new Region("la1", "la1"), new Region("la2", "la2"),
            new Region("oc1", "oc1"), new Region("ph2", "ph2"), new Region("ru", "ru"), new Region("sg2", "sg2"), new Region("th2", "th2"),
            new Region("tr1", "tr1"), new Region("tw2", "tw2"), new Region("vn2", "vn2") };

        private readonly StorageHelper _storageHelper;
        private readonly IConfiguration _config;

        public RiotUpdateHandler(StorageHelper storageHelper, IConfiguration config)
        {
            _storageHelper = storageHelper;
            _config = config;
        }

        public string HandlerName => "RiotUpdateHandler";

        public bool CanHandle(ServiceBusReceivedMessage message)
        {
            return string.Equals(message.Subject, "SummonerRegistered") && string.Equals(message.ReplyTo, "OauthCallback");
        }

        public async Task Handle(ServiceBusReceivedMessage message)
        {
            // Get the SummonerInfo object from the message
            SummonerInfo info = JsonSerializer.Deserialize<SummonerInfo>(message.Body);

            // Add any other fields that are in the table (ie, if we've already gotten the puuid, id, and region from previous runs)
            info = await _storageHelper.RetrieveCloudTable<SummonerInfo>("SummonerInfo", info.Reddit_Username, info.Flair_Subreddit);

            // If summoner puuid is blank, get it so we can get the rest of the info (region and summoner id).
            if (string.IsNullOrWhiteSpace(info.Summoner_Puuid))
            {
                RestClient restClient = new(_config.GetValue<string>("RIOT_BASE_URL"));
                RestRequest request = new("/riot/account/v1/accounts/me");
                request.AddHeader("Authorization", $"Bearer {info.Access_Token}");
                RestResponse response = await restClient.ExecuteAsync(request);

                // Token may be expired.  If so, refresh it.
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Get jwt for communicating with Riot.
                    JsonWebKey jsonPrivateKey = new JsonWebKey(_config.GetValue<string>("RIOT_SIGNING_KEY"));
                    SigningCredentials creds = new(jsonPrivateKey, SecurityAlgorithms.EcdsaSha256);

                    TokenForRiot riotToken = new()
                    {
                        Issuer = "487f4881-c787-4da6-bb2b-ce7cabbff312",
                        JwtId = Base64UrlEncoder.Encode(DateTime.UtcNow.ToString()),
                        Subject = "487f4881-c787-4da6-bb2b-ce7cabbff312"
                    };
                    riotToken.Audience.Add("https://auth.riotgames.com");

                    JsonWebTokenHandler tokenHandler = new();

                    string riotTokenString = tokenHandler.CreateToken(JsonSerializer.Serialize(riotToken), creds);

                    RestClient restClient2 = new(_config.GetValue<string>("RIOT_BASE_AUTH_URL"));
                    RestRequest request2 = new("/token", Method.Post);
                    request2.AddParameter("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
                    request2.AddParameter("client_assertion", riotTokenString);
                    request2.AddParameter("grant_type", "refresh_token");
                    request2.AddParameter("refresh_token", info.Refresh_Token);
                    request2.AddParameter("scope", "openid+cpid");

                    RestResponse response2 = await restClient2.ExecuteAsync(request2);

                    if (response2.IsSuccessful)
                    {
                        RiotRefreshTokenResult tokenResult = JsonSerializer.Deserialize<RiotRefreshTokenResult>(response2.Content);

                        info.Access_Token = tokenResult.Access_Token;
                        info.Refresh_Token = tokenResult.Refresh_Token;

                        _ = await _storageHelper.UpsertCloudTable("SummonerInfo", info);
                    }

                    request.AddHeader("Authorization", $"Bearer {info.Access_Token}");
                    response = await restClient.ExecuteAsync(request);
                }

                if (!response.IsSuccessful)
                {
                    // Something went wrong.  Why?
                    // Gonna have to find a good way to log this.
                    // For now, just exit, I guess.
                    return;
                }

                RiotAccountInfoResult accountResult = JsonSerializer.Deserialize<RiotAccountInfoResult>(response.Content);
                info.Summoner_Puuid = accountResult.Puuid;
                info.Summoner_Name = accountResult.Name;

                _ = await _storageHelper.UpsertCloudTable("SummonerInfo", info);
            }

            RiotApi api = RiotApi.NewInstance(_config.GetValue<string>("RIOT_API_KEY"));

            // Make sure we have the region
            if (string.IsNullOrEmpty(info.Region))
            {
                // Get region from Auth /userinfo.
                // Use access token.
                // Region is the cpid field.
                RestClient restClient = new(_config.GetValue<string>("RIOT_BASE_AUTH_URL"));
                RestRequest restRequest = new("/userinfo", Method.Get);
                restRequest.AddHeader("Authorization", $"Bearer {info.Access_Token}");

                RestResponse<UserInfoResponse> response = await restClient.ExecuteAsync<UserInfoResponse>(restRequest);

                if (response.IsSuccessful)
                {
                    if (!string.IsNullOrWhiteSpace(response?.Data?.CPID))
                    {
                        info.Region = response.Data.CPID.ToLowerInvariant();
                    }
                }
                else
                {
                    Console.WriteLine("No valid region determined.  Quitting.");
                }

                //foreach (Region region in regions)
                //{
                //    Summoner? summoner = await api.SummonerV4.GetByPUUIDAsync(region, info.Summoner_Puuid);
                //    if (summoner is not null)
                //    {
                //        info.Region = region.Key;
                //        info.Summoner_Id = summoner.Id;
                //        info.Summoner_Name = summoner.Name;
                //        await _storageHelper.UpsertCloudTable("SummonerInfo", info);
                //        break;
                //    }
                //}
            }

            Region infoRegion = new Region(info.Region, info.Region);

            // Make sure we have the Summoner Id.
            if (string.IsNullOrEmpty(info.Summoner_Id))
            {
                Summoner? summoner = new();// = await api.SummonerV4.GetByPUUIDAsync(infoRegion, info.Summoner_Puuid);
                try
                {
                    summoner = await api.SummonerV4.GetByPUUIDAsync(infoRegion, info.Summoner_Puuid);
                    if (summoner is null)
                    {
                        // Exit, I guess.
                        // Maybe deleted account?  I guess we'll play with it.
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                info.Summoner_Id = summoner.Id;
                info.Summoner_Name = summoner.Name;
                await _storageHelper.UpsertCloudTable("SummonerInfo", info);
            }

            // Get all the leagues, then pull out the RANKED_SOLO_5x5 league.
            LeagueEntry[] entries = await api.LeagueV4.GetLeagueEntriesForSummonerAsync(infoRegion, info.Summoner_Id);

            string rank = "Unranked";

            foreach (LeagueEntry entry in entries)
            {
                if (entry.QueueType.Equals("RANKED_SOLO_5x5", StringComparison.InvariantCultureIgnoreCase))
                {
                    string tier = entry.Tier.ToLower();
                    tier = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tier);
                    rank = $"{tier} {entry.Rank}";
                    break;
                }
            }
            
            // If the rank changed, update it.
            if (!info.League_Rank.Equals(rank))
            {
                info.League_Rank = rank;
                info.Signup_Timestamp = DateTime.UtcNow;
                _ = await _storageHelper.UpsertCloudTable("SummonerInfo", info);

                // Send message to Reddit Flair Updater to update the flair on reddit itself.
                ServiceBusMessage outboundMessage = new()
                {
                    ContentType = "application/json",
                    CorrelationId = message.CorrelationId,
                    ReplyTo = HandlerName,
                    Subject = $"SummonerRankUpdated",
                    Body = new BinaryData(JsonSerializer.Serialize(info))
                };

                await _storageHelper.SendServiceBusMessage(outboundMessage);
            }
        }
    }
}
