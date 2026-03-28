FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5007

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/DoctorScheduleService/DoctorScheduleService.csproj", "services/DoctorScheduleService/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "services/DoctorScheduleService/DoctorScheduleService.csproj"
COPY . .
WORKDIR "/src/services/DoctorScheduleService"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5007
ENTRYPOINT ["dotnet", "DoctorScheduleService.dll"]
