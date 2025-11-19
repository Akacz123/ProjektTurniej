# Etap 1: Budowanie aplikacji
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "./EsportsTournament.API.csproj"
RUN dotnet publish "./EsportsTournament.API.csproj" -c Release -o /app/publish

# Etap 2: Uruchomienie gotowej aplikacji
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "EsportsTournament.API.dll"]