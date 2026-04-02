FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/MedicalRecordServiceDotnet/MedicalRecordServiceDotnet.csproj", "services/MedicalRecordServiceDotnet/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/MedicalRecordServiceDotnet/MedicalRecordServiceDotnet.csproj"
COPY . .
WORKDIR "/src/services/MedicalRecordServiceDotnet"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MedicalRecordServiceDotnet.dll"]
