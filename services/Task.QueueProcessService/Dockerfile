FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Task.QueueProcessService.csproj ./
RUN dotnet restore Task.QueueProcessService.csproj

COPY . ./
RUN dotnet publish Task.QueueProcessService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Task.QueueProcessService.dll"]