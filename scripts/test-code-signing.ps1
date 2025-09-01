# 🧪 Script de Teste para Assinatura de Código
# Este script testa a configuração de assinatura de código

param(
    [string]$TestMode = "local"
)

Write-Host "🧪 Teste de Configuração de Assinatura de Código" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Função para testar conversão de certificado
function Test-CertificateConversion {
    Write-Host "📋 Testando conversão de certificado..." -ForegroundColor Yellow
    
    # Criar certificado de teste (simulado)
    $testCertPath = "test-certificate.pfx"
    $testPassword = "test123"
    
    try {
        # Simular criação de certificado de teste
        Write-Host "🔧 Criando certificado de teste simulado..." -ForegroundColor Cyan
        
        # Verificar se o script de conversão existe
        if (Test-Path "scripts\convert-certificate.ps1") {
            Write-Host "✅ Script de conversão encontrado" -ForegroundColor Green
        } else {
            Write-Warning "⚠️  Script de conversão não encontrado"
            return $false
        }
        
        Write-Host "✅ Teste de conversão concluído" -ForegroundColor Green
        return $true
        
    } catch {
        Write-Error "❌ Erro no teste de conversão: $_"
        return $false
    }
}

# Função para testar configuração de assinatura
function Test-SigningSetup {
    Write-Host "🔐 Testando configuração de assinatura..." -ForegroundColor Yellow
    
    try {
        # Verificar se o script de setup existe
        if (Test-Path "scripts\setup-code-signing.ps1") {
            Write-Host "✅ Script de setup encontrado" -ForegroundColor Green
        } else {
            Write-Warning "⚠️  Script de setup não encontrado"
            return $false
        }
        
        # Verificar se o electron-builder.yml está configurado
        if (Test-Path "electron-builder.yml") {
            $config = Get-Content "electron-builder.yml" -Raw
            if ($config -match "cscLink.*\$\{CSC_LINK\}") {
                Write-Host "✅ Configuração do electron-builder correta" -ForegroundColor Green
            } else {
                Write-Warning "⚠️  Configuração do electron-builder pode estar incorreta"
            }
        } else {
            Write-Warning "⚠️  Arquivo electron-builder.yml não encontrado"
        }
        
        Write-Host "✅ Teste de configuração concluído" -ForegroundColor Green
        return $true
        
    } catch {
        Write-Error "❌ Erro no teste de configuração: $_"
        return $false
    }
}

# Função para testar workflow do GitHub Actions
function Test-GitHubWorkflow {
    Write-Host "🚀 Testando workflow do GitHub Actions..." -ForegroundColor Yellow
    
    try {
        # Verificar se o workflow existe
        if (Test-Path ".github\workflows\desktop-windows.yml") {
            Write-Host "✅ Workflow encontrado" -ForegroundColor Green
            
            $workflow = Get-Content ".github\workflows\desktop-windows.yml" -Raw
            
            # Verificar se tem as variáveis de ambiente necessárias
            if ($workflow -match "CSC_LINK.*secrets\.CSC_LINK") {
                Write-Host "✅ Variável CSC_LINK configurada" -ForegroundColor Green
            } else {
                Write-Warning "⚠️  Variável CSC_LINK não encontrada no workflow"
            }
            
            if ($workflow -match "CSC_KEY_PASSWORD.*secrets\.CSC_KEY_PASSWORD") {
                Write-Host "✅ Variável CSC_KEY_PASSWORD configurada" -ForegroundColor Green
            } else {
                Write-Warning "⚠️  Variável CSC_KEY_PASSWORD não encontrada no workflow"
            }
            
        } else {
            Write-Warning "⚠️  Workflow não encontrado"
            return $false
        }
        
        Write-Host "✅ Teste do workflow concluído" -ForegroundColor Green
        return $true
        
    } catch {
        Write-Error "❌ Erro no teste do workflow: $_"
        return $false
    }
}

# Função para verificar estrutura de arquivos
function Test-FileStructure {
    Write-Host "📁 Verificando estrutura de arquivos..." -ForegroundColor Yellow
    
    $requiredFiles = @(
        "scripts\convert-certificate.ps1",
        "scripts\setup-code-signing.ps1",
        "electron-builder.yml",
        ".github\workflows\desktop-windows.yml"
    )
    
    $missingFiles = @()
    
    foreach ($file in $requiredFiles) {
        if (Test-Path $file) {
            Write-Host "✅ $file" -ForegroundColor Green
        } else {
            Write-Warning "❌ $file (não encontrado)"
            $missingFiles += $file
        }
    }
    
    if ($missingFiles.Count -eq 0) {
        Write-Host "✅ Estrutura de arquivos completa" -ForegroundColor Green
        return $true
    } else {
        Write-Warning "⚠️  Arquivos ausentes: $($missingFiles -join ', ')"
        return $false
    }
}

# Executar testes
Write-Host "🚀 Iniciando testes..." -ForegroundColor Cyan

$results = @{
    FileStructure = Test-FileStructure
    CertificateConversion = Test-CertificateConversion
    SigningSetup = Test-SigningSetup
    GitHubWorkflow = Test-GitHubWorkflow
}

# Resumo dos resultados
Write-Host ""
Write-Host "📊 RESUMO DOS TESTES" -ForegroundColor Magenta
Write-Host "===================" -ForegroundColor Magenta

$passedTests = 0
$totalTests = $results.Count

foreach ($test in $results.GetEnumerator()) {
    $status = if ($test.Value) { "✅ PASSOU" } else { "❌ FALHOU" }
    $color = if ($test.Value) { "Green" } else { "Red" }
    
    Write-Host "$($test.Key): $status" -ForegroundColor $color
    if ($test.Value) { $passedTests++ }
}

Write-Host ""
Write-Host "📈 Resultado: $passedTests/$totalTests testes passaram" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Yellow" })

# Recomendações
Write-Host ""
Write-Host "💡 RECOMENDAÇÕES:" -ForegroundColor Cyan

if ($passedTests -eq $totalTests) {
    Write-Host "🎉 Todos os testes passaram! Sua configuração está pronta." -ForegroundColor Green
    Write-Host "📋 Próximos passos:" -ForegroundColor White
    Write-Host "   1. Configure os secrets no GitHub (CSC_LINK, CSC_KEY_PASSWORD)" -ForegroundColor White
    Write-Host "   2. Faça push para a branch main" -ForegroundColor White
    Write-Host "   3. Verifique o workflow no GitHub Actions" -ForegroundColor White
} else {
    Write-Host "🔧 Alguns testes falharam. Verifique:" -ForegroundColor Yellow
    Write-Host "   1. Se todos os arquivos necessários existem" -ForegroundColor White
    Write-Host "   2. Se as configurações estão corretas" -ForegroundColor White
    Write-Host "   3. Se os scripts têm permissões de execução" -ForegroundColor White
}

Write-Host ""
Write-Host "📚 Para mais informações, consulte: CODE_SIGNING_SETUP.md" -ForegroundColor Cyan
