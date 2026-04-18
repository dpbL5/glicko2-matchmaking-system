FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Entity.MatchService.csproj ./
RUN dotnet restore Entity.MatchService.csproj

COPY . ./
RUN dotnet publish Entity.MatchService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Entity.MatchService.dll"]