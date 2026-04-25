FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5012

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/ImagingService/ImagingService.csproj", "services/ImagingService/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/ImagingService/ImagingService.csproj"
COPY . .
WORKDIR "/src/services/ImagingService"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5012
ENTRYPOINT ["dotnet", "ImagingService.dll"]
