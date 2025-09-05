# Script para testar endpoints especificos da API
# Este script testa os endpoints que o FirebirdAPI usa para se comunicar com o DashBirdServer

param(
    [string]$ServerUrl = "https://dashbird-server.holomn.com.br",
    [int]$Timeout = 30
)

Write-Host "Testando endpoints da API DashBirdServer..." -ForegroundColor Cyan
Write-Host "URL Base: $ServerUrl" -ForegroundColor Gray
Write-Host ""

# Lista de endpoints para testar
$endpoints = @(
    @{ Path = "/api/health"; Method = "GET"; Name = "Health Check" },
    @{ Path = "/api/User/register"; Method = "POST"; Name = "User Register" },
    @{ Path = "/api/User/login"; Method = "POST"; Name = "User Login" },
    @{ Path = "/api/AnonymousNode/register"; Method = "POST"; Name = "Anonymous Node Register" },
    @{ Path = "/api/User/profile"; Method = "GET"; Name = "User Profile" }
)

# Funcao para testar endpoint
function Test-ApiEndpoint {
    param(
        [string]$Url,
        [string]$Method,
        [string]$Name,
        [hashtable]$Body = $null
    )
    
    Write-Host "Testando $Name..." -ForegroundColor Yellow
    Write-Host "  $Method $Url" -ForegroundColor Gray
    
    try {
        $requestParams = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = $Timeout
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 3
            $requestParams.Body = $jsonBody
            $requestParams.ContentType = "application/json"
            Write-Host "  Body: $jsonBody" -ForegroundColor DarkGray
        }
        
        $response = Invoke-WebRequest @requestParams
        
        Write-Host "  SUCESSO Status: $($response.StatusCode)" -ForegroundColor Green
        
        # Mostrar headers importantes
        if ($response.Headers.ContainsKey("Content-Type")) {
            Write-Host "  Content-Type: $($response.Headers['Content-Type'])" -ForegroundColor DarkGray
        }
        
        # Mostrar parte da resposta se for pequena
        if ($response.Content.Length -lt 500) {
            Write-Host "  Response: $($response.Content)" -ForegroundColor DarkGray
        } else {
            Write-Host "  Response: $($response.Content.Substring(0, 200))..." -ForegroundColor DarkGray
        }
        
        return $true
    }
    catch {
        Write-Host "  ERRO: $($_.Exception.Message)" -ForegroundColor Red
        
        # Mostrar detalhes do erro se disponivel
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "  Status Code: $statusCode" -ForegroundColor DarkRed
        }
        
        return $false
    }
}

# Testar cada endpoint
$results = @{}

foreach ($endpoint in $endpoints) {
    $fullUrl = $ServerUrl + $endpoint.Path
    
    # Preparar body para endpoints POST
    $body = $null
    if ($endpoint.Method -eq "POST") {
        switch ($endpoint.Path) {
            "/api/User/register" {
                $body = @{
                    Name = "Test User"
                    Email = "test@example.com"
                    Password = "testpassword123"
                }
            }
            "/api/User/login" {
                $body = @{
                    Email = "test@example.com"
                    Password = "testpassword123"
                }
            }
            "/api/AnonymousNode/register" {
                $body = @{
                    MachineId = "test-machine-123"
                    MachineName = "Test Machine"
                    OperatingSystem = "Windows 10"
                    IpAddress = "192.168.1.100"
                }
            }
        }
    }
    
    $success = Test-ApiEndpoint -Url $fullUrl -Method $endpoint.Method -Name $endpoint.Name -Body $body
    $results[$endpoint.Name] = $success
    
    Write-Host ""
}

# Resumo dos testes
Write-Host "=== RESUMO DOS TESTES ===" -ForegroundColor Magenta
$totalTests = $results.Count
$passedTests = ($results.Values | Where-Object { $_ -eq $true }).Count
$failedTests = $totalTests - $passedTests

foreach ($result in $results.GetEnumerator()) {
    $status = if ($result.Value) { "SUCESSO" } else { "FALHOU" }
    $color = if ($result.Value) { "Green" } else { "Red" }
    Write-Host "$($result.Key): $status" -ForegroundColor $color
}

Write-Host ""
Write-Host "Total: $passedTests/$totalTests testes passaram" -ForegroundColor $(if ($failedTests -eq 0) { "Green" } else { "Yellow" })

if ($failedTests -eq 0) {
    Write-Host "TODOS OS ENDPOINTS ESTAO ACESSIVEIS!" -ForegroundColor Green
} else {
    Write-Host "ALGUNS ENDPOINTS FALHARAM! Verifique a configuracao do servidor." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Para testar com URL diferente, use:" -ForegroundColor Cyan
Write-Host ".\test-api-endpoints-final.ps1 -ServerUrl 'https://seu-servidor.com'" -ForegroundColor Gray
