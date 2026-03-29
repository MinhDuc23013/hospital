FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5084

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy project files first for layer caching
COPY ["gateway/HospitalGateway/HospitalGateway.csproj", "gateway/HospitalGateway/"]
COPY ["shared/HospitalShared/HospitalShared.csproj", "shared/HospitalShared/"]
RUN dotnet restore "gateway/HospitalGateway/HospitalGateway.csproj"
# Copy full source
COPY . .
WORKDIR "/src/gateway/HospitalGateway"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5084
ENTRYPOINT ["dotnet", "HospitalGateway.dll"]
