import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

console.log('🔍 Verificando build da API...');

// Verificar se a pasta api-dist existe
const apiDistPath = path.join(__dirname, '../api-dist');
if (fs.existsSync(apiDistPath)) {
  console.log('✅ Pasta api-dist encontrada');
  
  // Verificar se o executável existe
  const exePath = path.join(apiDistPath, 'FirebirdApi.exe');
  if (fs.existsSync(exePath)) {
    const stats = fs.statSync(exePath);
    console.log(`✅ FirebirdApi.exe encontrado (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
  } else {
    console.log('❌ FirebirdApi.exe não encontrado');
  }
  
  // Listar todos os arquivos
  const files = fs.readdirSync(apiDistPath);
  console.log('📁 Arquivos na pasta api-dist:');
  files.forEach(file => {
    const filePath = path.join(apiDistPath, file);
    const stats = fs.statSync(filePath);
    if (stats.isFile()) {
      console.log(`  - ${file} (${(stats.size / 1024).toFixed(2)} KB)`);
    } else {
      console.log(`  - ${file}/ (diretório)`);
    }
  });
} else {
  console.log('❌ Pasta api-dist não encontrada');
}

// Verificar se o build do Electron foi feito
const distPath = path.join(__dirname, '../dist');
if (fs.existsSync(distPath)) {
  console.log('✅ Pasta dist encontrada');
} else {
  console.log('❌ Pasta dist não encontrada');
}

const distElectronPath = path.join(__dirname, '../dist-electron');
if (fs.existsSync(distElectronPath)) {
  console.log('✅ Pasta dist-electron encontrada');
} else {
  console.log('❌ Pasta dist-electron não encontrada');
}

console.log('\n🎯 Para incluir a API no release, execute:');
console.log('   pnpm build:full');
console.log('   pnpm build:windows');



