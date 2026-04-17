FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Entity.PlayerService.csproj ./
RUN dotnet restore Entity.PlayerService.csproj

COPY . ./
RUN dotnet publish Entity.PlayerService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Entity.PlayerService.dll"]