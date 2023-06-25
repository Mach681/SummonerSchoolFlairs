using Azure;
using LeagueFlairRedditSignup.Classes;
using LeagueFlairRedditSignup.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using System.Security.Cryptography;
using System.Text;

namespace LeagueFlairRedditSignup.HostedService
{
    // Extend from IHostedService so the Host process in Program.cs will start and stop it for us.
    class RedditHandler : IHostedService
    {
        // Private/readonly variables that will be created from the constructor.
        // Note that the Host process automatically uses Dependency Injection to call the constructor
        //   and will also spawn instances of the required types for the constructor to succeed.
        private readonly Subreddit _subreddit;
        private readonly StorageHelper _storageHelper;

        // Constructor called by Dependency Injection as described in set of comments directly above this.
        public RedditHandler(IConfiguration config, RedditClient reddit, StorageHelper storageHelper)
        {
            _subreddit = reddit.Subreddit(config.GetValue<string>("SignupSubreddit"));
            _storageHelper = storageHelper;
        }

        // StartAsync gets called when the Host calls RunAsync.  This turns on the handler.
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Get current posts in the "new" category
            _subreddit.Posts.GetNew();

            // Setup event handler for any newly added posts
            _subreddit.Posts.NewUpdated += Posts_NewUpdated;

            // Turn on monitoring for new posts
            // By default this will poll every 1.5 seconds
            // If anything new is found this will trigger the NewUpdated posts event above
            //   and those posts will be sent to the Posts_NewUpdated handler (found below)
            _subreddit.Posts.MonitorNew();

            Console.WriteLine($"Monitoring new posts in {_subreddit.Name}");
            return Task.CompletedTask;
        }

        // StopAsync is called when the Host needs to quit.  This is cleanup.
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Shutting down.");

            // Cleanup
            // Disable monitoring for new posts
            _subreddit.Posts.MonitorNew();

            // Unregister event handler for new posts
            _subreddit.Posts.NewUpdated -= Posts_NewUpdated;

            return Task.CompletedTask;
        }

        // Posts_NewUpdated is wired in in StartAsync above and will handle any new posts the RedditClient finds.
        private void Posts_NewUpdated(object? sender, PostsUpdateEventArgs e)
        {
            // Loop to handle all the posts the RedditClient found since the last time.
            foreach (Post post in e.Added)
            {
                // LeagueFlairSignup is a class that will be turned into Json and stored in Azure Table Storage.
                // This represents the data that will sit in the table.
                LeagueFlairSignup user = new()
                {
                    SubredditName = _subreddit.Name,
                    UserName = post.Author,
                    PostId = post.Id,
                };

                // Create hash value for the status code in the oauth uri.  This will be the unique portion of the OAuth URL.
                // This unique portion is what allows us to know WHO the OAuth registration ties to on the Reddit side.
                // We'll use a hash of the post.author and the post.id (as salt to make it unique).
                Console.WriteLine($"New post by {post.Author}: {post.Title} with ID: {post.Id}");
                StringBuilder sb = new();
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{post.Id}{post.Author}"));

                    for (int i = 0; i < bytes.Length; i++)
                    {
                        sb.Append(bytes[i].ToString("x2"));
                    }
                }
                user.State = sb.ToString();

                // Save data (including unique hash) to table.  RiotOAuthCallback will use it.
                Response upsertResponse = _storageHelper.UpsertCloudTableAsync("RedditFlairUser", user).GetAwaiter().GetResult();

                // Create comment response to the post.
                Comment comment;

                if (upsertResponse.IsError)
                {
                    // If we can't save to table storage the process is dead already.  Send an error message and move on.
                    comment = post.Comment($"Something went wrong.  The error code is: {upsertResponse.Status} - {upsertResponse.ReasonPhrase}" +
                        $"\n\n" +
                        $"If this happens again, please contact the moderators.  Thank you!");
                }
                else
                {
                    // Create body of the comment, including te OAuth registration URL.
                    comment = post.Comment($"Thank you for requesting a flair!  To finish authentication, please sign in to your Riot Games account using the following link.  Note that this link is unique to you and you should never share it with anyone or use one someone else gives you." +
                        $"\n\n" +
                        $"Note: For security, always hover over the link and check that it starts with https://auth.riotgames.com." +
                        $"\n\n" +
                        $"https://auth.riotgames.com/authorize?redirect_uri=https://r-summonerschool.com/oauth-callback&client_id=487f4881-c787-4da6-bb2b-ce7cabbff312&response_type=code&scope=openid&state={user.State}" +
                        $"\n\n" +
                        $"This link will be valid for 2 days.");
                }

                // Submit (post) the comment.
                comment.Submit();
            }
        }
    }
}
