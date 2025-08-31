import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

console.log('🔍 Verificando build do DashBird...');

// Caminhos importantes
const releasePath = path.join(__dirname, '../release');
const distPath = path.join(__dirname, '../dist');
const distElectronPath = path.join(__dirname, '../dist-electron');
const apiDistPath = path.join(__dirname, '../api-dist');

// Verificações
const checks = [
  {
    name: 'Pasta dist (Vite build)',
    path: distPath,
    required: true,
    files: ['index.html', 'assets']
  },
  {
    name: 'Pasta dist-electron',
    path: distElectronPath,
    required: true,
    files: ['main.js', 'preload.mjs']
  },
  {
    name: 'Pasta api-dist',
    path: apiDistPath,
    required: true,
    files: ['FirebirdApi.exe']
  },
  {
    name: 'Pasta release (Electron Builder)',
    path: releasePath,
    required: true,
    files: ['*.exe']
  }
];

let allChecksPassed = true;

for (const check of checks) {
  console.log(`\n📁 Verificando: ${check.name}`);
  
  if (!fs.existsSync(check.path)) {
    if (check.required) {
      console.error(`❌ ${check.name} não encontrada: ${check.path}`);
      allChecksPassed = false;
    } else {
      console.log(`⚠️  ${check.name} não encontrada (opcional)`);
    }
    continue;
  }
  
  console.log(`✅ ${check.name} encontrada`);
  
  // Verificar arquivos específicos
  for (const file of check.files) {
    const filePath = path.join(check.path, file);
    if (file.includes('*')) {
      // Padrão glob - verificar se há arquivos que correspondem
      const files = fs.readdirSync(check.path).filter(f => f.includes(file.replace('*', '')));
      if (files.length === 0) {
        console.error(`❌ Nenhum arquivo encontrado para padrão: ${file}`);
        allChecksPassed = false;
      } else {
        console.log(`✅ ${files.length} arquivo(s) encontrado(s) para: ${file}`);
        files.forEach(f => console.log(`   - ${f}`));
      }
    } else if (fs.existsSync(filePath)) {
      const stats = fs.statSync(filePath);
      if (stats.isDirectory()) {
        console.log(`✅ Diretório encontrado: ${file}`);
      } else {
        console.log(`✅ Arquivo encontrado: ${file} (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
      }
    } else {
      console.error(`❌ Arquivo/diretório não encontrado: ${file}`);
      allChecksPassed = false;
    }
  }
}

// Verificação específica dos arquivos de release
if (fs.existsSync(releasePath)) {
  console.log('\n📦 Verificando arquivos de release:');
  const releaseFiles = fs.readdirSync(releasePath);
  
  const exeFiles = releaseFiles.filter(f => f.endsWith('.exe'));
  const zipFiles = releaseFiles.filter(f => f.endsWith('.zip'));
  const sevenZipFiles = releaseFiles.filter(f => f.endsWith('.7z'));
  
  console.log(`Executáveis (.exe): ${exeFiles.length}`);
  exeFiles.forEach(f => console.log(`   - ${f}`));
  
  console.log(`Arquivos ZIP (.zip): ${zipFiles.length}`);
  zipFiles.forEach(f => console.log(`   - ${f}`));
  
  console.log(`Arquivos 7z (.7z): ${sevenZipFiles.length}`);
  sevenZipFiles.forEach(f => console.log(`   - ${f}`));
  
  if (exeFiles.length === 0) {
    console.error('❌ Nenhum arquivo .exe encontrado na pasta release!');
    allChecksPassed = false;
  }
}

if (allChecksPassed) {
  console.log('\n✅ Todas as verificações passaram! Build está pronto.');
  process.exit(0);
} else {
  console.error('\n❌ Algumas verificações falharam. Verifique o build.');
  process.exit(1);
}



