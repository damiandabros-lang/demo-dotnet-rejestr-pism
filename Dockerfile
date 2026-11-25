FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RejestrPism.sln ./
COPY src/RejestrPism.Web/RejestrPism.Web.csproj src/RejestrPism.Web/
COPY tests/RejestrPism.Tests/RejestrPism.Tests.csproj tests/RejestrPism.Tests/
RUN dotnet restore
COPY . .
RUN dotnet test -c Release --no-restore -v q && dotnet publish src/RejestrPism.Web -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
USER app
HEALTHCHECK NONE
ENTRYPOINT ["dotnet", "RejestrPism.Web.dll"]
