# docker build --pull --rm -t feraloauthcallback:latest .
# docker run --rm -it -p 5000:5000 feraloauthcallback:latest sh
# docker run --rm -it -p 5000:5000 feraloauthcallback:latest
# docker run --rm -p 5000:5000 feraloauthcallback:latest
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
COPY . /app
WORKDIR /app

RUN dotnet build -c release -o /app/bin/release/net6.0

FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY --from=build-env /app/bin/release/net6.0 /app

WORKDIR /app
CMD dotnet /app/RiotOAuthCallback.dll

EXPOSE 5000