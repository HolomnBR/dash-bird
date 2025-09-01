# 🔐 Script de Conversão de Certificado para Assinatura de Código
# Este script converte certificados .pfx para o formato necessário para o GitHub Actions

param(
    [Parameter(Mandatory=$true)]
    [string]$CertificatePath,
    
    [Parameter(Mandatory=$true)]
    [string]$Password,
    
    [string]$OutputPath = "."
)

Write-Host "🔐 Conversor de Certificado para Assinatura de Código" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green

# Verificar se o arquivo existe
if (-not (Test-Path $CertificatePath)) {
    Write-Error "❌ Arquivo de certificado não encontrado: $CertificatePath"
    exit 1
}

try {
    Write-Host "📋 Lendo certificado: $CertificatePath" -ForegroundColor Yellow
    
    # Ler o arquivo .pfx como bytes
    $certBytes = Get-Content $CertificatePath -Encoding Byte
    
    Write-Host "✅ Certificado lido com sucesso" -ForegroundColor Green
    Write-Host "📊 Tamanho: $($certBytes.Length) bytes" -ForegroundColor Cyan
    
    # Converter para Base64
    Write-Host "🔄 Convertendo para Base64..." -ForegroundColor Yellow
    $base64 = [Convert]::ToBase64String($certBytes)
    
    # Criar URL de dados
    $dataUrl = "data:application/x-pkcs12;base64,$base64"
    
    Write-Host "✅ Conversão concluída!" -ForegroundColor Green
    
    # Salvar em arquivo
    $outputFile = Join-Path $OutputPath "certificate-base64.txt"
    $dataUrl | Out-File -FilePath $outputFile -Encoding UTF8
    
    Write-Host "💾 Resultado salvo em: $outputFile" -ForegroundColor Green
    
    # Exibir informações para o usuário
    Write-Host ""
    Write-Host "🔑 INFORMAÇÕES PARA O GITHUB ACTIONS:" -ForegroundColor Magenta
    Write-Host "=====================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "1. Acesse seu repositório no GitHub" -ForegroundColor White
    Write-Host "2. Vá para Settings > Secrets and variables > Actions" -ForegroundColor White
    Write-Host "3. Adicione os seguintes secrets:" -ForegroundColor White
    Write-Host ""
    Write-Host "   Secret Name: CSC_LINK" -ForegroundColor Cyan
    Write-Host "   Secret Value: (copie o conteúdo do arquivo $outputFile)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "   Secret Name: CSC_KEY_PASSWORD" -ForegroundColor Cyan
    Write-Host "   Secret Value: $Password" -ForegroundColor Cyan
    Write-Host ""
    
    # Verificar se o certificado é válido
    Write-Host "🔍 Verificando validade do certificado..." -ForegroundColor Yellow
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath, $Password)
        
        Write-Host "✅ Certificado válido!" -ForegroundColor Green
        Write-Host "📅 Válido de: $($cert.NotBefore)" -ForegroundColor Cyan
        Write-Host "📅 Válido até: $($cert.NotAfter)" -ForegroundColor Cyan
        Write-Host "👤 Emitido para: $($cert.Subject)" -ForegroundColor Cyan
        Write-Host "🏢 Emitido por: $($cert.Issuer)" -ForegroundColor Cyan
        
        # Calcular hash SHA1
        $sha1 = [System.Security.Cryptography.SHA1]::Create()
        $hash = $sha1.ComputeHash($certBytes)
        $sha1String = ($hash | ForEach-Object { $_.ToString("X2") }) -join ""
        
        Write-Host ""
        Write-Host "🔐 Hash SHA1 (opcional):" -ForegroundColor Magenta
        Write-Host "Secret Name: CSC_KEY_SHA1" -ForegroundColor Cyan
        Write-Host "Secret Value: $sha1String" -ForegroundColor Cyan
        
    } catch {
        Write-Warning "⚠️  Não foi possível verificar a validade do certificado: $_"
    }
    
    Write-Host ""
    Write-Host "🎉 Processo concluído com sucesso!" -ForegroundColor Green
    Write-Host "📝 Verifique o arquivo $outputFile para o valor do CSC_LINK" -ForegroundColor Yellow
    
} catch {
    Write-Error "❌ Erro durante a conversão: $_"
    exit 1
}
