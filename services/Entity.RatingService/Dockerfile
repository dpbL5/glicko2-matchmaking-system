FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Entity.RatingService.csproj ./
RUN dotnet restore Entity.RatingService.csproj

COPY . ./
RUN dotnet publish Entity.RatingService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5005
EXPOSE 5005

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Entity.RatingService.dll"]
