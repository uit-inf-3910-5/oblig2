FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
COPY deploy/ /app
WORKDIR /app
CMD dotnet Parser.dll
