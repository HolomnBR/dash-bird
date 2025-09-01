# 🔐 Script de Verificação de Assinatura Digital
# Este script verifica se os arquivos .exe estão corretamente assinados

param(
    [string]$Path = "release",
    [switch]$Verbose
)

Write-Host "🔐 Verificador de Assinatura Digital" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# Verificar se o diretório existe
if (-not (Test-Path $Path)) {
    Write-Error "❌ Diretório não encontrado: $Path"
    exit 1
}

# Função para verificar assinatura de um arquivo
function Test-FileSignature {
    param([string]$FilePath)
    
    try {
        $sig = Get-AuthenticodeSignature $FilePath
        
        switch ($sig.Status) {
            "Valid" { 
                Write-Host "✅ $($sig.StatusMessage)" -ForegroundColor Green
                return $true
            }
            "NotSigned" { 
                Write-Host "❌ Arquivo não assinado" -ForegroundColor Red
                return $false
            }
            "HashMismatch" { 
                Write-Host "⚠️  Hash não confere" -ForegroundColor Yellow
                return $false
            }
            "NotTrusted" { 
                Write-Host "⚠️  Certificado não confiável" -ForegroundColor Yellow
                return $false
            }
            default { 
                Write-Host "❓ Status desconhecido: $($sig.Status)" -ForegroundColor Red
                return $false
            }
        }
    } catch {
        Write-Error "❌ Erro ao verificar assinatura: $_"
        return $false
    }
}

# Função para exibir detalhes da assinatura
function Show-SignatureDetails {
    param([string]$FilePath)
    
    try {
        $sig = Get-AuthenticodeSignature $FilePath
        
        if ($sig.Status -eq "Valid") {
            Write-Host "   📋 Detalhes da Assinatura:" -ForegroundColor Cyan
            Write-Host "      Emitido para: $($sig.SignerCertificate.Subject)" -ForegroundColor White
            Write-Host "      Emitido por: $($sig.SignerCertificate.Issuer)" -ForegroundColor White
            Write-Host "      Válido de: $($sig.SignerCertificate.NotBefore)" -ForegroundColor White
            Write-Host "      Válido até: $($sig.SignerCertificate.NotAfter)" -ForegroundColor White
            Write-Host "      Serial Number: $($sig.SignerCertificate.SerialNumber)" -ForegroundColor White
            Write-Host "      Thumbprint: $($sig.SignerCertificate.Thumbprint)" -ForegroundColor White
        }
    } catch {
        Write-Warning "   ⚠️  Não foi possível obter detalhes da assinatura"
    }
}

# Contadores
$totalFiles = 0
$signedFiles = 0
$unsignedFiles = 0

Write-Host "🔍 Procurando arquivos executáveis em: $Path" -ForegroundColor Yellow

# Procurar por arquivos .exe
$exeFiles = Get-ChildItem -Path $Path -Filter "*.exe" -Recurse

if ($exeFiles.Count -eq 0) {
    Write-Host "⚠️  Nenhum arquivo .exe encontrado em: $Path" -ForegroundColor Yellow
    exit 0
}

Write-Host "📁 Encontrados $($exeFiles.Count) arquivo(s) .exe" -ForegroundColor Cyan
Write-Host ""

# Verificar cada arquivo
foreach ($file in $exeFiles) {
    $totalFiles++
    Write-Host "🔍 Verificando: $($file.Name)" -ForegroundColor White
    
    if (Test-FileSignature $file.FullName) {
        $signedFiles++
        if ($Verbose) {
            Show-SignatureDetails $file.FullName
        }
    } else {
        $unsignedFiles++
    }
    
    Write-Host ""
}

# Resumo
Write-Host "📊 RESUMO DA VERIFICAÇÃO:" -ForegroundColor Magenta
Write-Host "=========================" -ForegroundColor Magenta
Write-Host "📁 Total de arquivos: $totalFiles" -ForegroundColor White
Write-Host "✅ Arquivos assinados: $signedFiles" -ForegroundColor Green
Write-Host "❌ Arquivos não assinados: $unsignedFiles" -ForegroundColor Red

# Recomendações
Write-Host ""
if ($unsignedFiles -eq 0) {
    Write-Host "🎉 Todos os arquivos estão assinados digitalmente!" -ForegroundColor Green
    Write-Host "✅ Seu aplicativo deve funcionar sem problemas no Windows Defender SmartScreen" -ForegroundColor Green
} else {
    Write-Host "⚠️  ATENÇÃO: Existem arquivos não assinados!" -ForegroundColor Yellow
    Write-Host "🔐 Para resolver o problema do Windows Defender SmartScreen:" -ForegroundColor Yellow
    Write-Host "   1. Configure o certificado digital no GitHub Actions" -ForegroundColor White
    Write-Host "   2. Use o script convert-certificate.ps1 para preparar o certificado" -ForegroundColor White
    Write-Host "   3. Adicione os secrets CSC_LINK e CSC_KEY_PASSWORD" -ForegroundColor White
    Write-Host "   4. Execute o workflow novamente" -ForegroundColor White
}

Write-Host ""
Write-Host "📝 Para mais informações, consulte: CODE_SIGNING_SETUP.md" -ForegroundColor Cyan
