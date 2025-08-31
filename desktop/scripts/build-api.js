import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

console.log('🚀 Iniciando build da API .NET Core...');

// Caminhos
const apiProjectPath = path.join(__dirname, '../../FirebirdApi');
const outputPath = path.join(__dirname, '../api-dist');
const apiExePath = path.join(outputPath, 'FirebirdApi.exe');

try {
  // Limpar diretório de output anterior
  if (fs.existsSync(outputPath)) {
    fs.rmSync(outputPath, { recursive: true, force: true });
  }

  // Fazer build da API
  console.log('📦 Fazendo build da API...');
  execSync('dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false', {
    cwd: apiProjectPath,
    stdio: 'inherit'
  });

  // Copiar arquivos publicados para o diretório do projeto desktop
  const publishPath = path.join(apiProjectPath, 'bin/Release/net9.0/win-x64/publish');
  
  if (!fs.existsSync(publishPath)) {
    throw new Error('Diretório de publish não encontrado. Verifique se o build foi bem-sucedido.');
  }

  // Copiar todos os arquivos
  fs.cpSync(publishPath, outputPath, { recursive: true });

  // Verificar se o executável foi copiado corretamente
  if (!fs.existsSync(apiExePath)) {
    throw new Error('Executável da API não foi copiado corretamente');
  }

  const stats = fs.statSync(apiExePath);
  console.log('✅ API buildada com sucesso!');
  console.log(`📁 Arquivos copiados para: ${outputPath}`);
  console.log(`🔧 Executável: ${apiExePath} (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
  
  // Listar arquivos copiados
  const files = fs.readdirSync(outputPath);
  console.log('📦 Arquivos da API:');
  files.forEach(file => {
    const filePath = path.join(outputPath, file);
    const fileStats = fs.statSync(filePath);
    if (fileStats.isFile()) {
      console.log(`  - ${file} (${(fileStats.size / 1024).toFixed(2)} KB)`);
    } else {
      console.log(`  - ${file}/ (diretório)`);
    }
  });

} catch (error) {
  console.error('❌ Erro ao fazer build da API:', error.message);
  process.exit(1);
}
