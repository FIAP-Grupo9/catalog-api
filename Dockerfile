FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/FCG.Contracts/FCG.Contracts.csproj shared/FCG.Contracts/
COPY shared/FCG.BuildingBlocks/FCG.BuildingBlocks.csproj shared/FCG.BuildingBlocks/
COPY src/FCG.Catalog.Api/FCG.Catalog.Api.csproj src/FCG.Catalog.Api/
RUN dotnet restore src/FCG.Catalog.Api/FCG.Catalog.Api.csproj

COPY shared/ shared/
COPY . .
RUN dotnet publish src/FCG.Catalog.Api/FCG.Catalog.Api.csproj \
    -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FCG.Catalog.Api.dll"]
