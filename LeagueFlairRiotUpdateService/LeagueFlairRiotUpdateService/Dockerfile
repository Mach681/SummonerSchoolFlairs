# docker build --pull --rm -t leagueflairriotupdate:latest .
# docker run --rm -it leagueflairriotupdate:latest sh
# docker run --rm -it leagueflairriotupdate:latest
# docker run --rm leagueflairriotupdate:latest
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
COPY . /app
WORKDIR /app

RUN dotnet build -c release -o /app/bin/release/net6.0

FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY --from=build-env /app/bin/release/net6.0 /app

WORKDIR /app
CMD dotnet /app/LeagueFlairRiotUpdateService.dll

#EXPOSE 5000