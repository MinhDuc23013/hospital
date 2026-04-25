FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5011

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/LabTestService/LabTestService.csproj", "services/LabTestService/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/LabTestService/LabTestService.csproj"
COPY . .
WORKDIR "/src/services/LabTestService"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5011
ENTRYPOINT ["dotnet", "LabTestService.dll"]
