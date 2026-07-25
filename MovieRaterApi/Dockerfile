FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MovieRaterApi.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/logs

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS="http://+:8080"

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MovieRaterApi.dll"]
