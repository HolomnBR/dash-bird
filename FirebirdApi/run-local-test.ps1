# Script para executar o FirebirdAPI em modo de teste local
# Este script configura e executa o FirebirdAPI com configurações de teste

param(
    [string]$Environment = "LocalTest",
    [int]$Port = 8000,
    [switch]$SkipTests = $false,
    [switch]$Verbose = $false
)

Write-Host "🚀 INICIANDO FIREBIRD API EM MODO DE TESTE LOCAL" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Configurar variáveis de ambiente
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = "http://localhost:$Port"

Write-Host "Configuração:" -ForegroundColor Yellow
Write-Host "  Ambiente: $Environment" -ForegroundColor Gray
Write-Host "  Porta: $Port" -ForegroundColor Gray
Write-Host "  URL: http://localhost:$Port" -ForegroundColor Gray
Write-Host ""

# Verificar se o arquivo de configuração existe
$configFile = "appsettings.$Environment.json"
if (-not (Test-Path $configFile)) {
    Write-Host "❌ Arquivo de configuração não encontrado: $configFile" -ForegroundColor Red
    Write-Host "Criando arquivo de configuração padrão..." -ForegroundColor Yellow
    
    # Copiar appsettings.json como base
    if (Test-Path "appsettings.json") {
        Copy-Item "appsettings.json" $configFile
        Write-Host "✅ Arquivo $configFile criado baseado em appsettings.json" -ForegroundColor Green
    } else {
        Write-Host "❌ Arquivo appsettings.json não encontrado!" -ForegroundColor Red
        exit 1
    }
}

# Executar testes de conectividade se não for pulado
if (-not $SkipTests) {
    Write-Host "Executando testes de conectividade..." -ForegroundColor Yellow
    
    $testScript = ".\test-integration.ps1"
    if (Test-Path $testScript) {
        try {
            & $testScript -Verbose:$Verbose
            if ($LASTEXITCODE -ne 0) {
                Write-Host "⚠️  Alguns testes falharam, mas continuando..." -ForegroundColor Yellow
            }
        } catch {
            Write-Host "⚠️  Erro ao executar testes: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  Script de teste não encontrado: $testScript" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# Verificar se o projeto está compilado
Write-Host "Verificando se o projeto está compilado..." -ForegroundColor Yellow
if (-not (Test-Path "bin\Debug\net8.0\FirebirdApi.dll")) {
    Write-Host "Compilando projeto..." -ForegroundColor Yellow
    try {
        & dotnet build --configuration Debug --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Falha ao compilar o projeto!" -ForegroundColor Red
            exit 1
        }
        Write-Host "✅ Projeto compilado com sucesso!" -ForegroundColor Green
    } catch {
        Write-Host "❌ Erro ao compilar: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Executar o FirebirdAPI
Write-Host "Iniciando FirebirdAPI..." -ForegroundColor Yellow
Write-Host "Acesse: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Swagger: http://localhost:$Port/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pressione Ctrl+C para parar o servidor" -ForegroundColor Gray
Write-Host ""

try {
    & dotnet run --environment $Environment --urls "http://localhost:$Port"
} catch {
    Write-Host "❌ Erro ao executar FirebirdAPI: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "FirebirdAPI parado." -ForegroundColor Yellow
