# Script para limpar tokens armazenados localmente
Write-Host "🧹 Limpando tokens armazenados localmente..." -ForegroundColor Yellow

# Caminho do diretório de dados da aplicação
$dataDir = [System.IO.Path]::Combine([Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData), "DashBird")
$tokenFilePath = [System.IO.Path]::Combine($dataDir, "auth_token.json")

if (Test-Path $tokenFilePath) {
    Remove-Item $tokenFilePath -Force
    Write-Host "✅ Token removido: $tokenFilePath" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Nenhum token encontrado em: $tokenFilePath" -ForegroundColor Blue
}

Write-Host "🎯 Tokens limpos! Reinicie o FirebirdAPI para aplicar as mudanças." -ForegroundColor Green
