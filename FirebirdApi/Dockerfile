# Use a imagem oficial do .NET 9.0 SDK para build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar arquivos de projeto
COPY ["FirebirdApi.csproj", "./"]
RUN dotnet restore "FirebirdApi.csproj"

# Copiar todo o código fonte
COPY . .

# Build da aplicação
RUN dotnet build "FirebirdApi.csproj" -c Release -o /app/build

# Publish da aplicação
FROM build AS publish
RUN dotnet publish "FirebirdApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagem final de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Instalar dependências do sistema para Firebird
RUN apt-get update && apt-get install -y \
    libfbclient2 \
    libfbclient-dev \
    && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=publish /app/publish .

# Criar diretório para bases de dados
RUN mkdir -p /app/dbs

# Expor porta
EXPOSE 5000
EXPOSE 5001

# Configurar variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicialização
ENTRYPOINT ["dotnet", "FirebirdApi.dll"]
