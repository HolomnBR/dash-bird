# 🔐 Script de Configuração de Assinatura de Código para GitHub Actions
# Este script configura o ambiente para assinatura de código no CI/CD

param(
    [string]$CertificateBase64,
    [string]$CertificatePassword,
    [string]$WorkingDirectory = "."
)

Write-Host "🔐 Configurador de Assinatura de Código para GitHub Actions" -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Green

# Mudar para o diretório de trabalho
if ($WorkingDirectory -ne ".") {
    Set-Location $WorkingDirectory
}

try {
    # Verificar se temos o certificado
    if (-not $CertificateBase64) {
        Write-Warning "⚠️  Nenhum certificado fornecido. Continuando sem assinatura digital."
        return
    }

    Write-Host "📋 Configurando certificado de assinatura..." -ForegroundColor Yellow
    
    # Criar diretório para certificado se não existir
    $privateDir = "private"
    if (-not (Test-Path $privateDir)) {
        New-Item -ItemType Directory -Path $privateDir -Force | Out-Null
        Write-Host "✅ Diretório $privateDir criado" -ForegroundColor Green
    }
    
    # Processar o certificado Base64
    $certData = $CertificateBase64
    
    # Se o certificado está em formato data URL, extrair apenas a parte Base64
    if ($certData.StartsWith("data:application/x-pkcs12;base64,")) {
        $base64Part = $certData.Substring("data:application/x-pkcs12;base64,".Length)
        Write-Host "📋 Certificado detectado em formato data URL" -ForegroundColor Cyan
    } else {
        $base64Part = $certData
        Write-Host "📋 Certificado detectado em formato Base64 puro" -ForegroundColor Cyan
    }
    
    # Decodificar Base64 para bytes
    Write-Host "🔄 Decodificando certificado Base64..." -ForegroundColor Yellow
    $certBytes = [Convert]::FromBase64String($base64Part)
    
    # Salvar como arquivo .pfx
    $certPath = Join-Path $privateDir "certificate.pfx"
    [IO.File]::WriteAllBytes($certPath, $certBytes)
    
    Write-Host "✅ Certificado salvo em: $certPath" -ForegroundColor Green
    Write-Host "📊 Tamanho do arquivo: $((Get-Item $certPath).Length) bytes" -ForegroundColor Cyan
    
    # Verificar se o certificado é válido
    Write-Host "🔍 Verificando validade do certificado..." -ForegroundColor Yellow
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath, $CertificatePassword)
        
        Write-Host "✅ Certificado válido!" -ForegroundColor Green
        Write-Host "📅 Válido de: $($cert.NotBefore)" -ForegroundColor Cyan
        Write-Host "📅 Válido até: $($cert.NotAfter)" -ForegroundColor Cyan
        Write-Host "👤 Emitido para: $($cert.Subject)" -ForegroundColor Cyan
        Write-Host "🏢 Emitido por: $($cert.Issuer)" -ForegroundColor Cyan
        
        # Calcular hash SHA1
        $sha1 = [System.Security.Cryptography.SHA1]::Create()
        $hash = $sha1.ComputeHash($certBytes)
        $sha1String = ($hash | ForEach-Object { $_.ToString("X2") }) -join ""
        
        Write-Host "🔐 Hash SHA1: $sha1String" -ForegroundColor Magenta
        
    } catch {
        Write-Warning "⚠️  Não foi possível verificar a validade do certificado: $_"
        Write-Host "🔄 Continuando mesmo assim..." -ForegroundColor Yellow
    }
    
    # Configurar variáveis de ambiente para o electron-builder
    $env:CSC_LINK = $certPath
    $env:CSC_KEY_PASSWORD = $CertificatePassword
    
    Write-Host ""
    Write-Host "🔑 VARIÁVEIS DE AMBIENTE CONFIGURADAS:" -ForegroundColor Magenta
    Write-Host "=====================================" -ForegroundColor Magenta
    Write-Host "CSC_LINK: $env:CSC_LINK" -ForegroundColor Cyan
    Write-Host "CSC_KEY_PASSWORD: [CONFIGURADO]" -ForegroundColor Cyan
    
    # Verificar se as variáveis estão configuradas
    if ($env:CSC_LINK -and $env:CSC_KEY_PASSWORD) {
        Write-Host ""
        Write-Host "🎉 Configuração concluída com sucesso!" -ForegroundColor Green
        Write-Host "✅ O electron-builder agora pode assinar o aplicativo" -ForegroundColor Green
    } else {
        Write-Warning "⚠️  Algumas variáveis não foram configuradas corretamente"
    }
    
} catch {
    Write-Error "❌ Erro durante a configuração: $_"
    Write-Host "🔄 Continuando sem assinatura digital..." -ForegroundColor Yellow
    
    # Limpar variáveis em caso de erro
    $env:CSC_LINK = $null
    $env:CSC_KEY_PASSWORD = $null
}
