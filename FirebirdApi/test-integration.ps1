# Script de teste de integração completo
# Este script executa todos os testes de conectividade e integração

param(
    [string]$ServerUrl = "https://dashbird-server.holomn.com.br",
    [string]$GrpcUrl = "https://dashbird-server-grpc.holomn.com.br",
    [int]$Timeout = 30,
    [switch]$SkipGrpc = $false,
    [switch]$SkipApi = $false,
    [switch]$Verbose = $false
)

Write-Host "🚀 INICIANDO TESTE DE INTEGRAÇÃO COMPLETO" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Configurar logging se verbose estiver ativado
if ($Verbose) {
    $VerbosePreference = "Continue"
}

# Função para executar script e capturar resultado
function Invoke-TestScript {
    param(
        [string]$ScriptPath,
        [string]$TestName,
        [hashtable]$Parameters = @{}
    )
    
    Write-Host "Executando: $TestName" -ForegroundColor Yellow
    Write-Host "Script: $ScriptPath" -ForegroundColor Gray
    
    try {
        $paramString = ""
        foreach ($param in $Parameters.GetEnumerator()) {
            $paramString += " -$($param.Key) '$($param.Value)'"
        }
        
        $command = "& '$ScriptPath'$paramString"
        $result = Invoke-Expression $command
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $TestName - SUCESSO" -ForegroundColor Green
            return $true
        } else {
            Write-Host "❌ $TestName - FALHOU (Exit Code: $LASTEXITCODE)" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "❌ $TestName - ERRO: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Obter diretório do script atual
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Lista de testes para executar
$tests = @()

# Teste 1: Conectividade básica
if (-not $SkipApi) {
    $tests += @{
        Name = "Teste de Conectividade Básica"
        Script = Join-Path $scriptDir "test-connection.ps1"
        Parameters = @{
            ServerUrl = $ServerUrl
            GrpcUrl = $GrpcUrl
            Timeout = $Timeout
        }
    }
}

# Teste 2: Endpoints da API
if (-not $SkipApi) {
    $tests += @{
        Name = "Teste de Endpoints da API"
        Script = Join-Path $scriptDir "test-api-endpoints.ps1"
        Parameters = @{
            ServerUrl = $ServerUrl
            Timeout = $Timeout
        }
    }
}

# Teste 3: Conectividade gRPC
if (-not $SkipGrpc) {
    $tests += @{
        Name = "Teste de Conectividade gRPC"
        Script = Join-Path $scriptDir "test-grpc-connection.ps1"
        Parameters = @{
            GrpcUrl = $GrpcUrl
            Timeout = $Timeout
        }
    }
}

# Executar todos os testes
$results = @{}
$testNumber = 1

foreach ($test in $tests) {
    Write-Host ""
    Write-Host "=== TESTE $testNumber/$($tests.Count): $($test.Name) ===" -ForegroundColor Magenta
    
    $success = Invoke-TestScript -ScriptPath $test.Script -TestName $test.Name -Parameters $test.Parameters
    $results[$test.Name] = $success
    
    $testNumber++
}

# Resumo final
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "📊 RESUMO FINAL DOS TESTES" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

$totalTests = $results.Count
$passedTests = ($results.Values | Where-Object { $_ -eq $true }).Count
$failedTests = $totalTests - $passedTests

foreach ($result in $results.GetEnumerator()) {
    $status = if ($result.Value) { "✅ PASSOU" } else { "❌ FALHOU" }
    $color = if ($result.Value) { "Green" } else { "Red" }
    Write-Host "$($result.Key): $status" -ForegroundColor $color
}

Write-Host ""
Write-Host "Estatísticas:" -ForegroundColor Yellow
Write-Host "  Total de testes: $totalTests" -ForegroundColor White
Write-Host "  Testes passaram: $passedTests" -ForegroundColor Green
Write-Host "  Testes falharam: $failedTests" -ForegroundColor Red

$successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
Write-Host "  Taxa de sucesso: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } elseif ($successRate -ge 60) { "Yellow" } else { "Red" })

Write-Host ""

if ($failedTests -eq 0) {
    Write-Host "🎉 TODOS OS TESTES PASSARAM!" -ForegroundColor Green
    Write-Host "O servidor está pronto para receber conexões do FirebirdAPI." -ForegroundColor Green
    exit 0
} elseif ($successRate -ge 80) {
    Write-Host "⚠️  MAIORIA DOS TESTES PASSOU" -ForegroundColor Yellow
    Write-Host "O servidor está funcionando, mas alguns recursos podem ter problemas." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "❌ MUITOS TESTES FALHARAM" -ForegroundColor Red
    Write-Host "O servidor pode não estar funcionando corretamente." -ForegroundColor Red
    exit 2
}

Write-Host ""
Write-Host "Para executar testes específicos, use:" -ForegroundColor Cyan
Write-Host ".\test-integration.ps1 -SkipGrpc  # Pular testes gRPC" -ForegroundColor Gray
Write-Host ".\test-integration.ps1 -SkipApi   # Pular testes de API" -ForegroundColor Gray
Write-Host ".\test-integration.ps1 -Verbose   # Modo verboso" -ForegroundColor Gray
