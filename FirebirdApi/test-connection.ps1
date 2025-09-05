# Script para testar conectividade com o DashBirdServer
# Execute este script para testar a conexão antes de fazer deploy

param(
    [string]$ServerUrl = "https://dashbird-server.holomn.com.br",
    [string]$GrpcUrl = "https://dashbird-server-grpc.holomn.com.br",
    [int]$Timeout = 30
)

Write-Host "🔍 Testando conectividade com DashBirdServer..." -ForegroundColor Cyan
Write-Host ""

# Função para testar conectividade HTTP
function Test-HttpConnection {
    param([string]$Url, [string]$TestName)
    
    Write-Host "Testando $TestName..." -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec $Timeout -UseBasicParsing
        Write-Host "✅ $TestName - Status: $($response.StatusCode)" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ $TestName - Erro: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Função para testar conectividade gRPC (simulação)
function Test-GrpcConnection {
    param([string]$Url, [string]$TestName)
    
    Write-Host "Testando $TestName..." -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        # Para gRPC, vamos testar se a porta está aberta
        $uri = [System.Uri]$Url
        $host = $uri.Host
        $port = if ($uri.Port -ne -1) { $uri.Port } else { if ($uri.Scheme -eq "https") { 443 } else { 80 } }
        
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connect = $tcpClient.BeginConnect($host, $port, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne($Timeout * 1000, $false)
        
        if ($wait) {
            $tcpClient.EndConnect($connect)
            Write-Host "✅ $TestName - Porta $port acessível" -ForegroundColor Green
            $tcpClient.Close()
            return $true
        } else {
            Write-Host "❌ $TestName - Timeout na porta $port" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ $TestName - Erro: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Teste 1: API REST
Write-Host "=== TESTE 1: API REST ===" -ForegroundColor Magenta
$apiTest = Test-HttpConnection -Url "$ServerUrl/api/health" -TestName "API REST Health Check"
if (-not $apiTest) {
    $apiTest = Test-HttpConnection -Url "$ServerUrl" -TestName "API REST Root"
}

# Teste 2: gRPC
Write-Host ""
Write-Host "=== TESTE 2: gRPC ===" -ForegroundColor Magenta
$grpcTest = Test-GrpcConnection -Url $GrpcUrl -TestName "gRPC Port Check"

# Teste 3: DNS Resolution
Write-Host ""
Write-Host "=== TESTE 3: DNS RESOLUTION ===" -ForegroundColor Magenta
try {
    $dnsResult = Resolve-DnsName -Name "dashbird-server.holomn.com.br" -ErrorAction Stop
    Write-Host "✅ DNS Resolution - IP: $($dnsResult[0].IPAddress)" -ForegroundColor Green
    $dnsTest = $true
} catch {
    Write-Host "❌ DNS Resolution - Erro: $($_.Exception.Message)" -ForegroundColor Red
    $dnsTest = $false
}

# Teste 4: SSL Certificate
Write-Host ""
Write-Host "=== TESTE 4: SSL CERTIFICATE ===" -ForegroundColor Magenta
try {
    $request = [System.Net.WebRequest]::Create($ServerUrl)
    $request.Timeout = $Timeout * 1000
    $response = $request.GetResponse()
    $response.Close()
    Write-Host "✅ SSL Certificate - Válido" -ForegroundColor Green
    $sslTest = $true
} catch {
    Write-Host "❌ SSL Certificate - Erro: $($_.Exception.Message)" -ForegroundColor Red
    $sslTest = $false
}

# Resumo dos testes
Write-Host ""
Write-Host "=== RESUMO DOS TESTES ===" -ForegroundColor Magenta
Write-Host "API REST: $(if ($apiTest) { '✅ OK' } else { '❌ FALHOU' })" -ForegroundColor $(if ($apiTest) { 'Green' } else { 'Red' })
Write-Host "gRPC: $(if ($grpcTest) { '✅ OK' } else { '❌ FALHOU' })" -ForegroundColor $(if ($grpcTest) { 'Green' } else { 'Red' })
Write-Host "DNS: $(if ($dnsTest) { '✅ OK' } else { '❌ FALHOU' })" -ForegroundColor $(if ($dnsTest) { 'Green' } else { 'Red' })
Write-Host "SSL: $(if ($sslTest) { '✅ OK' } else { '❌ FALHOU' })" -ForegroundColor $(if ($sslTest) { 'Green' } else { 'Red' })

$allTestsPassed = $apiTest -and $grpcTest -and $dnsTest -and $sslTest

Write-Host ""
if ($allTestsPassed) {
    Write-Host "🎉 TODOS OS TESTES PASSARAM! O servidor está acessível." -ForegroundColor Green
} else {
    Write-Host "⚠️  ALGUNS TESTES FALHARAM! Verifique a conectividade do servidor." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Para testar com URLs diferentes, use:" -ForegroundColor Cyan
Write-Host ".\test-connection.ps1 -ServerUrl 'https://seu-servidor.com' -GrpcUrl 'https://seu-servidor-grpc.com'" -ForegroundColor Gray
