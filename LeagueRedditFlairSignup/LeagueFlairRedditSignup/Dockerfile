# docker build --pull --rm -t leagueflairreddit:latest .
# docker run --rm -it leagueflairreddit:latest sh
# docker run --rm -it leagueflairreddit:latest
# docker run --rm leagueflairreddit:latest
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
COPY . /app
WORKDIR /app

RUN dotnet build -c release -o /app/bin/release/net6.0

FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY --from=build-env /app/bin/release/net6.0 /app

WORKDIR /app
CMD dotnet /app/LeagueFlairRedditSignup.dll
