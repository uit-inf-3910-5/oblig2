FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY deploy/ /app
WORKDIR /app
CMD dotnet Parser.dll
