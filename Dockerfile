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
# O MongoDB.Driver sonda o suporte a GSSAPI/Kerberos no startup. Nao usamos esse
# mecanismo (a autenticacao e SCRAM), mas sem a biblioteca o driver imprime um
# "Error: libgssapi_krb5.so.2" nos logs que aparenta falha e nao e.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FCG.Catalog.Api.dll"]
