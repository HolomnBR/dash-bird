# Script para build do Windows com privilégios de administrador
# Este script resolve problemas de permissão com links simbólicos

Write-Host "🔧 Executando build do Windows com privilégios de administrador..." -ForegroundColor Green

# Verificar se estamos executando como administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "⚠️  Este script precisa ser executado como administrador para resolver problemas de permissão" -ForegroundColor Yellow
    Write-Host "💡 Execute: Start-Process PowerShell -Verb RunAs" -ForegroundColor Cyan
    exit 1
}

Write-Host "✅ Executando como administrador" -ForegroundColor Green

# Limpar cache do electron-builder para evitar problemas
Write-Host "🧹 Limpando cache do electron-builder..." -ForegroundColor Yellow
$cachePath = "$env:LOCALAPPDATA\electron-builder\Cache"
if (Test-Path $cachePath) {
    Remove-Item -Path $cachePath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Cache limpo" -ForegroundColor Green
}

# Executar build da API
Write-Host "📦 Executando build da API..." -ForegroundColor Yellow
pnpm run build:api

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro no build da API" -ForegroundColor Red
    exit 1
}

# Executar build do Electron
Write-Host "⚡ Executando build do Electron..." -ForegroundColor Yellow
pnpm run build:electron

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro no build do Electron" -ForegroundColor Red
    exit 1
}

# Executar build final com electron-builder
Write-Host "🚀 Executando build final..." -ForegroundColor Yellow
pnpm run build:windows

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Erro no build final" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build concluído com sucesso!" -ForegroundColor Green

# Verificar se a API foi incluída
Write-Host "🔍 Verificando se a API foi incluída no pacote..." -ForegroundColor Yellow
$apiPath = "release\win-unpacked\resources\api-dist\FirebirdApi.exe"
if (Test-Path $apiPath) {
    Write-Host "✅ API encontrada no pacote: $apiPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  API não encontrada no pacote. Verificando outras localizações..." -ForegroundColor Yellow
    
    # Verificar se está em resources
    $resourcesPath = "release\win-unpacked\resources"
    if (Test-Path $resourcesPath) {
        $apiFiles = Get-ChildItem -Path $resourcesPath -Recurse -Name "*FirebirdApi*" -ErrorAction SilentlyContinue
        if ($apiFiles) {
            Write-Host "✅ Arquivos da API encontrados:" -ForegroundColor Green
            $apiFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Cyan }
        } else {
            Write-Host "❌ Nenhum arquivo da API encontrado no pacote" -ForegroundColor Red
        }
    }
}

Write-Host "🎉 Processo concluído!" -ForegroundColor Green
