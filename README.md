# SummonerSchoolFlairs
Set of microservices that run the flair bot for the /r/summonerschool subreddit.

The LeagueFlairRedditSignup project has a lot of notes for anyone wondering how the flair process works.  It assumes very limited knowledge of C#, so it overexplains a lot.  Start in Program.cs to follow the notes.

The system is 5 microservices.  They are as follows (in order that anyone would interact with them).

LeagueFlairRedditSignup

This service monitors the signup subreddit (feralflairs) for new posts and has the bot respond with an unique OAuth signup link.

RiotOAuthCallback

Signing in with an OAuth link sends the sign in information to a web page.  That's what this application is.  It is VERY basic, but will match the League user to the Reddit user (using the unique value from the signup link) and save the information to Azure Table Storage for future processing.  It also sends an Azure Service Bus message to the LeagueFlairRiotUpdateService to check the actual rank of the summoner.

LeagueFlairRiotUpdateService

This service will take the credentials from the RiotOAuthCallback OAuth sign in and use it to get the current rank for the summoner who signed up.  If the existing rank on this record doesn't match the current rank (ie, no rank was previously saved, or the last rank was Silver II, but now the summoner is Gold IV) it will save the current rank and send an Azure Service Bus Message to the next service, LeagueFlairRedditUpdateService.

LeagueFlairRedditUpdateService

This service is only going to process users who have had their rank change.  It will pull the current rank from Azure Table Storage, get the corresponding Reddit user's flair and make sure the user's flair is correct, updating it if necessary.

LeagueFlairUpdateJob

This service is a cron job that will run every few minutes and check for various maintenance things that need to be done.  For example, any OAuth link that hasn't been used in over 2 days is expired, so it will delete them.  Any summoner registered for over 1 year will also be cleared and need to re-register.  The most important thing, though, is to get a list of accounts that haven't been updated in a period of time and send a message to the LeagueFlairRiotUpdateService to check if the saved rank is still the correct rank.  If not, that service will update it and trigger the Reddit update as well.  This is critical to keeping the flairs up to date.


The names of the services are all over the place because I did most of the coding between 12:30 and 2:30 AM.  There are also dozens of shortcuts and bad practices in there (hard coded values that should be moved to the appsettings.json, extra values that aren't needed, etc.).  I may or may not ever update things there.  If they bother you, feel free to make a pull request.

This readme probably looks like trash right now.  If I ever care enough to update the formatting, I will.  I'll also try to add more comments to the other services besides the LeagueRedditFlairSignup.
