FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/PharmacyServiceDotnet/PharmacyServiceDotnet.csproj", "services/PharmacyServiceDotnet/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/PharmacyServiceDotnet/PharmacyServiceDotnet.csproj"
COPY . .
WORKDIR "/src/services/PharmacyServiceDotnet"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5004
ENTRYPOINT ["dotnet", "PharmacyServiceDotnet.dll"]
