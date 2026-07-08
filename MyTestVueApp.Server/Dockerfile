FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MyTestVueApp.Server/MyTestVueApp.Server.csproj MyTestVueApp.Server/
RUN dotnet restore MyTestVueApp.Server/MyTestVueApp.Server.csproj

COPY MyTestVueApp.Server/ MyTestVueApp.Server/
RUN dotnet publish MyTestVueApp.Server/MyTestVueApp.Server.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet MyTestVueApp.Server.dll"]
