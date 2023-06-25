using Azure.Messaging.ServiceBus;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using RiotOAuthCallback.Classes;
using RiotOAuthCallback.Helpers;
using System.Text;
using System.Text.Json;

WebApplication app = WebApplication.CreateBuilder(args).Build();

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddJsonFile("local.appsettings.json", optional: true, reloadOnChange: true)
    .Build();

RestClient restClient = new RestClient(config.GetValue<string>("RIOT_BASE_URL"));
ServiceBusClient sbclient = new ServiceBusClient(config.GetValue<string>("AzureServiceBusConnectionString"));
StorageHelper storageHelper = new StorageHelper(config, sbclient);
JsonWebKey jsonPrivateKey = new JsonWebKey(config.GetValue<string>("RIOT_SIGNING_KEY"));

app.MapGet("/oauth-callback", async Task<IResult> (HttpContext context) =>
{
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

    string code = context.Request.Query["code"];

    RestRequest request = new RestRequest("/token", Method.Post);

    request.AddParameter("code", code);
    request.AddParameter("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer");
    request.AddParameter("client_assertion", riotTokenString);
    request.AddParameter("grant_type", "authorization_code");
    request.AddParameter("redirect_uri", config.GetValue<string>("CALLBACK_URL"));
    request.AddParameter("client_id", config.GetValue<string>("CLIENT_ID"));

    RestResponse<TokenResponse?> response = restClient.Execute<TokenResponse?>(request);

    string responseError = "Wasatch";

    if (response.IsSuccessStatusCode)
    {
        responseError = "Oquirrh";
        TokenResponse? tokenResponse = response.Data;

        if (tokenResponse is not null)
        {
            responseError = "Wind Rivers";
            List<RedditFlairUser> redditUserList = await storageHelper.QueryCloudTableAsync<RedditFlairUser>("RedditFlairUser", x => string.Equals(x.State, context.Request.Query["state"]));

            responseError = "Adirondacks";
            if (redditUserList is not null && redditUserList.Count == 1)
            {
                responseError = "Urals";
                RedditFlairUser user = redditUserList[0];

                // Check expiration date
                if (user.ExpiresOn < DateTimeOffset.UtcNow)
                {
                    _ = await storageHelper.DeleteCloudTableAsync("RedditFlairUser", user);
                    return Results.Text("This link is expired.  Please try to register again.");
                }

                responseError = "Andes";
                SummonerInfo info = new()
                {
                    Access_Token = tokenResponse.Access_Token,
                    Id_Token = tokenResponse.Id_Token,
                    Refresh_Token = tokenResponse.Refresh_Token,
                    Reddit_Username = user.UserName,
                    Flair_Subreddit = config.GetValue<string>("FlairSubreddit")
                };
                
                responseError = "Alps";
                await storageHelper.UpsertCloudTableAsync("SummonerInfo", info);
                ServiceBusMessage message = new ServiceBusMessage()
                {
                    ContentType = "application/json",
                    CorrelationId = Guid.NewGuid().ToString(),
                    ReplyTo = "OauthCallback",
                    Subject = "SummonerRegistered",
                    Body = new BinaryData(JsonSerializer.Serialize(info))
                };

                responseError = "Appalachians";
                await storageHelper.SendServiceBusMessageAsync(message, "summonerregistered");

                // Delete the link
                responseError = "Himalayas";
                _ = await storageHelper.DeleteCloudTableAsync("RedditFlairUser", user);

                return Results.Content("<!DOCTYPE html><html><head><title>Summoner School Flair Successfully Registered!</title><meta http-equiv = \"refresh\" content = \"3; url = https://www.reddit.com/r/summonerschool/\" /></head><body><p>You've successfully registered!  This page will redirect to https://www.reddit.com/r/summonerschool in 3 seconds.</p></body></html>", "text/html", Encoding.UTF8);
            }
        }
    }

    return Results.Text($"Something went wrong.  Please try again.  If this continues to happen, please contact the moderators and give them this error: {responseError}");
});

app.Run();
