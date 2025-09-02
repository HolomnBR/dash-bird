# Script de limpeza completa para mudança de branch
# Remove todos os caches, builds e dependências para garantir build limpo

param(
    [switch]$Force,
    [switch]$SkipConfirmation
)

Write-Host "🧹 Dash Bird - Script de Limpeza Completa" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

if (-not $SkipConfirmation -and -not $Force) {
    $confirmation = Read-Host "⚠️  Isso irá remover TODOS os caches, builds e dependências. Continuar? (y/N)"
    if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
        Write-Host "❌ Operação cancelada pelo usuário." -ForegroundColor Red
        exit 0
    }
}

Write-Host "`n🚀 Iniciando limpeza completa..." -ForegroundColor Yellow

# Função para remover diretório com verificação
function Remove-DirectorySafe {
    param($Path, $Description)
    
    if (Test-Path $Path) {
        Write-Host "🗑️  Removendo $Description..." -ForegroundColor Yellow
        try {
            Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
            Write-Host "✅ $Description removido com sucesso" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Erro ao remover $Description : $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "ℹ️  $Description não encontrado, pulando..." -ForegroundColor Gray
    }
}

# Função para limpar cache do pnpm
function Clear-PnpmCache {
    Write-Host "`n📦 Limpando cache do pnpm..." -ForegroundColor Yellow
    try {
        pnpm store prune
        Write-Host "✅ Cache do pnpm limpo" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Erro ao limpar cache do pnpm: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Função para limpar cache do .NET
function Clear-DotNetCache {
    Write-Host "`n🔧 Limpando cache do .NET..." -ForegroundColor Yellow
    try {
        dotnet clean --verbosity quiet
        dotnet nuget locals all --clear
        Write-Host "✅ Cache do .NET limpo" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Erro ao limpar cache do .NET: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 1. Limpar diretórios de build do frontend (desktop/)
Write-Host "`n🖥️  Limpando builds do frontend..." -ForegroundColor Cyan
Remove-DirectorySafe -Path "desktop/node_modules" -Description "node_modules do desktop"
Remove-DirectorySafe -Path "desktop/dist" -Description "dist do Vite"
Remove-DirectorySafe -Path "desktop/dist-electron" -Description "dist-electron"
Remove-DirectorySafe -Path "desktop/release" -Description "release builds"
Remove-DirectorySafe -Path "desktop/api-dist" -Description "api-dist"

# 2. Limpar diretórios de build do backend (.NET)
Write-Host "`n🔧 Limpando builds do backend..." -ForegroundColor Cyan
Remove-DirectorySafe -Path "FirebirdApi/bin" -Description "bin do FirebirdApi"
Remove-DirectorySafe -Path "FirebirdApi/obj" -Description "obj do FirebirdApi"
Remove-DirectorySafe -Path "FirebirdTest/bin" -Description "bin do FirebirdTest"
Remove-DirectorySafe -Path "FirebirdTest/obj" -Description "obj do FirebirdTest"

# 3. Limpar node_modules da raiz (se existir)
Write-Host "`n📁 Limpando dependências da raiz..." -ForegroundColor Cyan
Remove-DirectorySafe -Path "node_modules" -Description "node_modules da raiz"

# 4. Limpar caches específicos
Write-Host "`n💾 Limpando caches..." -ForegroundColor Cyan

# Cache do Vite
Remove-DirectorySafe -Path "desktop/.vite" -Description "cache do Vite"

# Cache do TypeScript
Remove-DirectorySafe -Path "desktop/.tsbuildinfo" -Description "cache do TypeScript"

# Cache do Electron Builder
Remove-DirectorySafe -Path "desktop/app-builds" -Description "cache do Electron Builder"

# Cache do pnpm
Clear-PnpmCache

# Cache do .NET
Clear-DotNetCache

# 5. Limpar arquivos temporários
Write-Host "`n🗂️  Limpando arquivos temporários..." -ForegroundColor Cyan

# Arquivos de lock que podem estar desatualizados
$lockFiles = @(
    "desktop/pnpm-lock.yaml",
    "pnpm-lock.yaml"
)

foreach ($lockFile in $lockFiles) {
    if (Test-Path $lockFile) {
        Write-Host "🗑️  Removendo $lockFile..." -ForegroundColor Yellow
        Remove-Item -Path $lockFile -Force -ErrorAction SilentlyContinue
    }
}

# 6. Verificar se há processos em execução
Write-Host "`n🔍 Verificando processos em execução..." -ForegroundColor Cyan

$processes = @("electron", "FirebirdApi", "node")
foreach ($process in $processes) {
    $running = Get-Process -Name $process -ErrorAction SilentlyContinue
    if ($running) {
        Write-Host "⚠️  Processo $process está em execução. Considere finalizá-lo." -ForegroundColor Yellow
    }
}

Write-Host "`n🎉 Limpeza completa finalizada!" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

Write-Host "`n📋 Próximos passos recomendados:" -ForegroundColor Cyan
Write-Host "1. Instalar dependências: pnpm install" -ForegroundColor White
Write-Host "2. Build da API: cd FirebirdApi && dotnet build" -ForegroundColor White
Write-Host "3. Build do frontend: cd desktop && pnpm build" -ForegroundColor White
Write-Host "4. Ou usar o build completo: cd desktop && pnpm build:full" -ForegroundColor White

Write-Host "`n💡 Dica: Use 'pnpm run clean-branch' para executar este script facilmente!" -ForegroundColor Yellow
