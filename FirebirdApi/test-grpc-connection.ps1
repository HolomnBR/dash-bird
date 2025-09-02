# Script para testar a conexão gRPC
Write-Host "🧪 Testando conexão com servidor gRPC..." -ForegroundColor Cyan

# URL do servidor gRPC
$grpcServerUrl = "https://localhost:7001"
$healthEndpoint = "https://localhost:5000/health/grpc"

Write-Host "📡 Servidor gRPC: $grpcServerUrl" -ForegroundColor Yellow
Write-Host "🏥 Endpoint de teste: $healthEndpoint" -ForegroundColor Yellow

# Testar se o servidor gRPC está rodando
Write-Host "`n🔍 Verificando se o servidor gRPC está acessível..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri $grpcServerUrl -Method GET -TimeoutSec 5 -SkipCertificateCheck
    Write-Host "✅ Servidor gRPC está respondendo" -ForegroundColor Green
} catch {
    Write-Host "❌ Servidor gRPC não está acessível: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Certifique-se de que o servidor DashBird está rodando em $grpcServerUrl" -ForegroundColor Yellow
}

# Testar endpoint de health da API
Write-Host "`n🔍 Testando endpoint de health da API..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "https://localhost:5000/health" -Method GET -TimeoutSec 5 -SkipCertificateCheck
    Write-Host "✅ API está respondendo" -ForegroundColor Green
    $healthData = $response.Content | ConvertFrom-Json
    Write-Host "   Status: $($healthData.status)" -ForegroundColor Green
} catch {
    Write-Host "❌ API não está acessível: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Execute 'dotnet run' na pasta FirebirdApi para iniciar a API" -ForegroundColor Yellow
}

Write-Host "`n📋 Próximos passos:" -ForegroundColor Cyan
Write-Host "1. Certifique-se de que o servidor DashBird está rodando em $grpcServerUrl" -ForegroundColor White
Write-Host "2. Execute 'dotnet run' na pasta FirebirdApi para iniciar a API cliente" -ForegroundColor White
Write-Host "3. Acesse $healthEndpoint para testar a conexão gRPC" -ForegroundColor White
