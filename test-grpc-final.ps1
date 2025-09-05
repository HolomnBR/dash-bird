# Script final para testar gRPC com HTTPS
Write-Host "🔍 Testando conexão gRPC com HTTPS..." -ForegroundColor Yellow

# Testar servidor DashBird HTTP
Write-Host "1. Verificando servidor DashBird (HTTP)..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001/status" -Method Get -TimeoutSec 5
    Write-Host "✅ Servidor DashBird HTTP: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ Servidor DashBird HTTP: FALHOU" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
}

# Testar servidor DashBird HTTPS
Write-Host "2. Verificando servidor DashBird (HTTPS)..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "https://localhost:7001/status" -Method Get -TimeoutSec 5 -SkipCertificateCheck
    Write-Host "✅ Servidor DashBird HTTPS: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ Servidor DashBird HTTPS: FALHOU" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
}

# Testar FirebirdApi
Write-Host "3. Verificando FirebirdApi..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method Get -TimeoutSec 5
    Write-Host "✅ FirebirdApi: OK" -ForegroundColor Green
} catch {
    Write-Host "❌ FirebirdApi: FALHOU" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Testar gRPC
Write-Host "4. Testando conexão gRPC..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health/grpc" -Method Get -TimeoutSec 15
    $json = $response.Content | ConvertFrom-Json
    Write-Host "✅ gRPC Status: $($json.grpc_connection)" -ForegroundColor Green
    Write-Host "📝 Mensagem: $($json.message)" -ForegroundColor White
    if ($json.error) {
        Write-Host "⚠️ Erro detalhado: $($json.error)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ gRPC: FALHOU" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "🏁 Teste concluído!" -ForegroundColor Yellow
