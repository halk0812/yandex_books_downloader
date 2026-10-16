FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY cs/ ./
RUN dotnet publish src/YandexBooksDownloader.Cli/YandexBooksDownloader.Cli.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /data
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "/app/yandex-books-downloader.dll"]
