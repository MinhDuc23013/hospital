FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/SearchServiceDotnet/SearchServiceDotnet.csproj", "services/SearchServiceDotnet/"]
RUN dotnet restore "services/SearchServiceDotnet/SearchServiceDotnet.csproj"
COPY . .
WORKDIR "/src/services/SearchServiceDotnet"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SearchServiceDotnet.dll"]
