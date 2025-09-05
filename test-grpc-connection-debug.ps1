# Script para testar conexão gRPC com debug detalhado
# Este script ajuda a diagnosticar problemas de conexão gRPC

Write-Host "🔍 Testando conexão gRPC com debug detalhado..." -ForegroundColor Cyan

# Configurações
$grpcServerUrl = "https://dashbird-server-grpc.holomn.com.br"
$cloudServerUrl = "https://dashbird-server.holomn.com.br"
$localGrpcUrl = "https://localhost:7001"
$localHttpUrl = "http://localhost:7001"

Write-Host "`n📋 Configurações de teste:" -ForegroundColor Yellow
Write-Host "  gRPC Server URL: $grpcServerUrl"
Write-Host "  Cloud Server URL: $cloudServerUrl"
Write-Host "  Local gRPC URL: $localGrpcUrl"
Write-Host "  Local HTTP URL: $localHttpUrl"

# Teste 1: Verificar se o servidor cloud está acessível
Write-Host "`n🌐 Teste 1: Verificando acessibilidade do servidor cloud..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$cloudServerUrl/status" -Method GET -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ✅ Servidor cloud acessível: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  📄 Resposta: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Servidor cloud não acessível: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 2: Verificar se o servidor gRPC está acessível
Write-Host "`n📡 Teste 2: Verificando acessibilidade do servidor gRPC..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$grpcServerUrl/status" -Method GET -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ✅ Servidor gRPC acessível: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  📄 Resposta: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Servidor gRPC não acessível: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 3: Verificar servidor local (se estiver rodando)
Write-Host "`n🏠 Teste 3: Verificando servidor local..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$localHttpUrl/status" -Method GET -TimeoutSec 5 -UseBasicParsing
    Write-Host "  ✅ Servidor local HTTP acessível: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  📄 Resposta: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Servidor local HTTP não acessível: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 4: Verificar certificados SSL
Write-Host "`n🔒 Teste 4: Verificando certificados SSL..." -ForegroundColor Yellow
try {
    $request = [System.Net.WebRequest]::Create($grpcServerUrl)
    $request.Timeout = 10000
    $response = $request.GetResponse()
    $cert = $request.ServicePoint.Certificate
    Write-Host "  ✅ Certificado SSL válido para $grpcServerUrl" -ForegroundColor Green
    Write-Host "  📄 Certificado: $($cert.Subject)" -ForegroundColor Gray
    $response.Close()
} catch {
    Write-Host "  ❌ Erro de certificado SSL: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 5: Testar conexão gRPC via API local
Write-Host "`n🔧 Teste 5: Testando conexão gRPC via API local..." -ForegroundColor Yellow
try {
    $apiUrl = "http://localhost:5000/api/health/grpc"
    $response = Invoke-WebRequest -Uri $apiUrl -Method GET -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ✅ API local acessível: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  📄 Resposta: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ API local não acessível: $($_.Exception.Message)" -ForegroundColor Red
}

# Teste 6: Verificar configuração do cliente
Write-Host "`n⚙️ Teste 6: Verificando configuração do cliente..." -ForegroundColor Yellow
$configFile = "appsettings.json"
if (Test-Path $configFile) {
    $config = Get-Content $configFile | ConvertFrom-Json
    Write-Host "  📄 Configuração encontrada:" -ForegroundColor Green
    Write-Host "    GrpcServer.Url: $($config.GrpcServer.Url)" -ForegroundColor Gray
    Write-Host "    CloudServer.Url: $($config.CloudServer.Url)" -ForegroundColor Gray
    Write-Host "    Environment.Name: $($config.Environment.Name)" -ForegroundColor Gray
} else {
    Write-Host "  ❌ Arquivo de configuração não encontrado: $configFile" -ForegroundColor Red
}

# Teste 7: Verificar se há token de autenticação
Write-Host "`n🔑 Teste 7: Verificando token de autenticação..." -ForegroundColor Yellow
try {
    $tokenUrl = "http://localhost:5000/api/auth/status"
    $response = Invoke-WebRequest -Uri $tokenUrl -Method GET -TimeoutSec 5 -UseBasicParsing
    Write-Host "  ✅ Status de autenticação: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  📄 Resposta: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Não foi possível verificar status de autenticação: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎯 Resumo dos testes:" -ForegroundColor Cyan
Write-Host "  - Verifique se o servidor cloud está rodando e acessível"
Write-Host "  - Verifique se há problemas de certificado SSL"
Write-Host "  - Verifique se o token de autenticação é válido"
Write-Host "  - Considere usar o servidor local para desenvolvimento"

Write-Host "`n💡 Próximos passos:" -ForegroundColor Yellow
Write-Host "  1. Se o servidor cloud não estiver acessível, use o servidor local"
Write-Host "  2. Se houver problemas de SSL, verifique os certificados"
Write-Host "  3. Se houver problemas de autenticação, verifique o token JWT"
Write-Host "  4. Execute o servidor local: cd DashBirdServer && dotnet run"