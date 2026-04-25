FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5010

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/OrchestratorService/OrchestratorService.csproj", "services/OrchestratorService/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/OrchestratorService/OrchestratorService.csproj"
COPY . .
WORKDIR "/src/services/OrchestratorService"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5010
ENTRYPOINT ["dotnet", "OrchestratorService.dll"]
