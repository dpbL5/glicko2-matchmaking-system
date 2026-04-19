FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Task.MatchmakingProcessService.csproj ./
RUN dotnet restore Task.MatchmakingProcessService.csproj

COPY . ./
RUN dotnet publish Task.MatchmakingProcessService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Task.MatchmakingProcessService.dll"]
