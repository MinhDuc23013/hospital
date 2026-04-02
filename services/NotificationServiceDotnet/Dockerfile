FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/NotificationServiceDotnet/NotificationServiceDotnet.csproj", "services/NotificationServiceDotnet/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/NotificationServiceDotnet/NotificationServiceDotnet.csproj"
COPY . .
WORKDIR "/src/services/NotificationServiceDotnet"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationServiceDotnet.dll"]
