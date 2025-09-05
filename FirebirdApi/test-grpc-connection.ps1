# Teste de conectividade gRPC
Write-Host "🔍 Testando conectividade com servidor gRPC..." -ForegroundColor Yellow

$grpcUrl = "https://dashbird-server-grpc.holomn.com.br"
$cloudUrl = "https://dashbird-server.holomn.com.br"

Write-Host "`n📡 Testando servidor gRPC: $grpcUrl" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri $grpcUrl -Method GET -TimeoutSec 10 -UseBasicParsing
    Write-Host "✅ Servidor gRPC respondeu: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content-Type: $($response.Headers['Content-Type'])" -ForegroundColor Gray
} catch {
    Write-Host "❌ Erro ao conectar no servidor gRPC: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🌐 Testando servidor Cloud: $cloudUrl" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri $cloudUrl -Method GET -TimeoutSec 10 -UseBasicParsing
    Write-Host "✅ Servidor Cloud respondeu: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content-Type: $($response.Headers['Content-Type'])" -ForegroundColor Gray
} catch {
    Write-Host "❌ Erro ao conectar no servidor Cloud: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🔧 Testando endpoints específicos..." -ForegroundColor Cyan

# Testar endpoint de status do servidor Cloud
try {
    $statusUrl = "$cloudUrl/status"
    $response = Invoke-WebRequest -Uri $statusUrl -Method GET -TimeoutSec 10 -UseBasicParsing
    Write-Host "✅ Status endpoint respondeu: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Erro no status endpoint: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏁 Teste concluído!" -ForegroundColor Yellow