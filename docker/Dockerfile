# Passo 1: Restore (copia apenas os .csproj para aproveitar cache de camadas)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /app

COPY ./src/FiapEsperancaSolidaria.Campanha.slnx ./

COPY ./src/FiapEsperancaSolidaria.Campanha.Api/*.csproj ./FiapEsperancaSolidaria.Campanha.Api/
COPY ./src/FiapEsperancaSolidaria.Campanha.Application/*.csproj ./FiapEsperancaSolidaria.Campanha.Application/
COPY ./src/FiapEsperancaSolidaria.Campanha.Domain/*.csproj ./FiapEsperancaSolidaria.Campanha.Domain/
COPY ./src/FiapEsperancaSolidaria.Campanha.Infrastructure/*.csproj ./FiapEsperancaSolidaria.Campanha.Infrastructure/
COPY ./src/FiapEsperancaSolidaria.Campanha.Observability/*.csproj ./FiapEsperancaSolidaria.Campanha.Observability/
COPY ./src/FiapEsperancaSolidaria.Campanha.Queue/*.csproj ./FiapEsperancaSolidaria.Campanha.Queue/
COPY ./src/FiapEsperancaSolidaria.Campanha.Tests/*.csproj ./FiapEsperancaSolidaria.Campanha.Tests/

RUN dotnet restore FiapEsperancaSolidaria.Campanha.slnx

# Passo 2: Build (copia o restante do código e compila, reaproveitando o cache do restore)
FROM restore AS build
WORKDIR /app

COPY ./src/. .

RUN dotnet build FiapEsperancaSolidaria.Campanha.slnx \
    -c Release \
    --no-restore

# Passo 3: Test (roda os testes unitários como parte do build da imagem)
FROM build AS test
RUN dotnet test FiapEsperancaSolidaria.Campanha.slnx \
    -c Release \
    --no-build \
    --verbosity normal

# Passo 4: Publish (gera os artefatos de publicação)
FROM build AS publish
RUN dotnet publish FiapEsperancaSolidaria.Campanha.Api/FiapEsperancaSolidaria.Campanha.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-build

# Passo 5: Runtime (imagem final, só com os artefatos publicados)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FiapEsperancaSolidaria.Campanha.Api.dll"]

# docker build -f docker/Dockerfile -t esperanca-solidaria/campanha-api:latest .
# docker run -d -p 8080:8080 --name campanha-api esperanca-solidaria/campanha-api:latest
