# Script para testar validação JWT
# Este script ajuda a verificar se o token JWT está sendo validado corretamente

Write-Host "🔑 Testando validação JWT..." -ForegroundColor Cyan

# Configurações
$cloudServerUrl = "https://dashbird-server.holomn.com.br"
$localServerUrl = "http://localhost:5001"

Write-Host "`n📋 Configurações de teste:" -ForegroundColor Yellow
Write-Host "  Cloud Server URL: $cloudServerUrl"
Write-Host "  Local Server URL: $localServerUrl"

# Teste 1: Verificar se o servidor está rodando
Write-Host "`n🌐 Teste 1: Verificando se o servidor está rodando..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$localServerUrl/status" -Method GET -TimeoutSec 5 -UseBasicParsing
    Write-Host "  ✅ Servidor local acessível: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Servidor local não acessível: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  💡 Execute: cd DashBirdServer && dotnet run" -ForegroundColor Yellow
    exit 1
}

# Teste 2: Testar endpoint de registro de usuário (sem autenticação)
Write-Host "`n👤 Teste 2: Testando registro de usuário..." -ForegroundColor Yellow
$userData = @{
    name = "Test User"
    email = "test@example.com"
    password = "TestPassword123!"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$localServerUrl/api/User/register" -Method POST -Body $userData -ContentType "application/json" -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ✅ Registro de usuário: $($response.StatusCode)" -ForegroundColor Green
    $result = $response.Content | ConvertFrom-Json
    Write-Host "  📄 Resposta: $($result.message)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Erro no registro: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorContent)
        $errorText = $reader.ReadToEnd()
        Write-Host "  📄 Detalhes: $errorText" -ForegroundColor Gray
    }
}

# Teste 3: Testar login de usuário
Write-Host "`n🔐 Teste 3: Testando login de usuário..." -ForegroundColor Yellow
$loginData = @{
    email = "test@example.com"
    password = "TestPassword123!"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$localServerUrl/api/User/login" -Method POST -Body $loginData -ContentType "application/json" -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ✅ Login de usuário: $($response.StatusCode)" -ForegroundColor Green
    $result = $response.Content | ConvertFrom-Json
    $token = $result.token
    Write-Host "  🔑 Token obtido: $($token.Substring(0, 20))..." -ForegroundColor Gray
    
    # Teste 4: Testar endpoint protegido com token
    Write-Host "`n🛡️ Teste 4: Testando endpoint protegido com token..." -ForegroundColor Yellow
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$localServerUrl/api/User/profile" -Method GET -Headers $headers -TimeoutSec 10 -UseBasicParsing
        Write-Host "  ✅ Endpoint protegido acessível: $($response.StatusCode)" -ForegroundColor Green
        $result = $response.Content | ConvertFrom-Json
        Write-Host "  📄 Perfil: $($result.name) ($($result.email))" -ForegroundColor Gray
    } catch {
        Write-Host "  ❌ Erro no endpoint protegido: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "  ❌ Erro no login: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorContent)
        $errorText = $reader.ReadToEnd()
        Write-Host "  📄 Detalhes: $errorText" -ForegroundColor Gray
    }
}

# Teste 5: Testar registro de nó anônimo
Write-Host "`n🔗 Teste 5: Testando registro de nó anônimo..." -ForegroundColor Yellow
$nodeData = @{
    name = "Test Node"
    machineName = "TEST-MACHINE"
    machineId = "test-machine-123"
    ipAddress = "127.0.0.1"
    port = 5000
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$localServerUrl/api/AnonymousNode/register" -Method POST -Body $nodeData -ContentType "application/json" -TimeoutSec 10 -UseBasicParsing
    Write-Host "  ✅ Registro de nó anônimo: $($response.StatusCode)" -ForegroundColor Green
    $result = $response.Content | ConvertFrom-Json
    Write-Host "  📄 Nó ID: $($result.id)" -ForegroundColor Gray
    Write-Host "  📄 Token anônimo: $($result.anonymousToken)" -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Erro no registro de nó anônimo: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorContent)
        $errorText = $reader.ReadToEnd()
        Write-Host "  📄 Detalhes: $errorText" -ForegroundColor Gray
    }
}

Write-Host "`n🎯 Resumo dos testes JWT:" -ForegroundColor Cyan
Write-Host "  - Verifique se o servidor está rodando localmente"
Write-Host "  - Verifique se o registro e login de usuários funcionam"
Write-Host "  - Verifique se os tokens JWT são gerados corretamente"
Write-Host "  - Verifique se os endpoints protegidos funcionam com token"

Write-Host "`n💡 Se os testes falharem:" -ForegroundColor Yellow
Write-Host "  1. Verifique se o servidor DashBird está rodando"
Write-Host "  2. Verifique se o banco de dados está acessível"
Write-Host "  3. Verifique os logs do servidor para erros"
Write-Host "  4. Considere usar o servidor local para desenvolvimento"
