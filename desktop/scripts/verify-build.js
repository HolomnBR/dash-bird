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

// Função para encontrar executáveis recursivamente
function findExecutables(dirPath) {
  if (!fs.existsSync(dirPath)) return [];
  
  const executables = [];
  
  function scanDirectory(currentPath) {
    const items = fs.readdirSync(currentPath);
    
    items.forEach(item => {
      const fullPath = path.join(currentPath, item);
      const stats = fs.statSync(fullPath);
      
      if (stats.isDirectory()) {
        scanDirectory(fullPath);
      } else if (item.endsWith('.exe')) {
        executables.push({
          name: item,
          path: fullPath,
          size: stats.size
        });
      }
    });
  }
  
  scanDirectory(dirPath);
  return executables;
}

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
  
  // Verificar se os arquivos da API estão presentes na pasta release
  console.log('\n🔍 Verificando arquivos da API na pasta release:');
  const apiExeInRelease = path.join(releasePath, 'FirebirdApi.exe');
  if (fs.existsSync(apiExeInRelease)) {
    const stats = fs.statSync(apiExeInRelease);
    console.log(`✅ FirebirdApi.exe encontrado em release (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
  } else {
    console.log('⚠️  FirebirdApi.exe não encontrado em release, mas pode estar em subpasta');
    
    // Verificar se está em subpasta api-dist
    const apiDistInRelease = path.join(releasePath, 'api-dist', 'FirebirdApi.exe');
    if (fs.existsSync(apiDistInRelease)) {
      const stats = fs.statSync(apiDistInRelease);
      console.log(`✅ FirebirdApi.exe encontrado em release/api-dist (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
    } else {
      console.log('⚠️  FirebirdApi.exe não encontrado em release/api-dist');
    }
  }
  
  // Verificar se os arquivos do Electron estão presentes na pasta release
  console.log('\n🔍 Verificando arquivos do Electron na pasta release:');
  const distInRelease = path.join(releasePath, 'dist');
  const distElectronInRelease = path.join(releasePath, 'dist-electron');
  
  if (fs.existsSync(distInRelease)) {
    console.log('✅ Pasta dist encontrada em release');
    const distFiles = fs.readdirSync(distInRelease);
    console.log(`   - ${distFiles.length} arquivo(s)/pasta(s) encontrado(s)`);
  }
  
  if (fs.existsSync(distElectronInRelease)) {
    console.log('✅ Pasta dist-electron encontrada em release');
    const distElectronFiles = fs.readdirSync(distElectronInRelease);
    console.log(`   - ${distElectronFiles.length} arquivo(s)/pasta(s) encontrado(s)`);
  }
  
  // Verificar especificamente o instalador e o executável descompactado
  console.log('\n🎯 Verificando artefatos de build específicos:');
  const setupFiles = releaseFiles.filter(f => f.match(/^Dash Bird Setup.*\.exe$/));

  if (setupFiles.length > 0) {
    const setupFile = setupFiles[0];
    const setupFilePath = path.join(releasePath, setupFile);
    const stats = fs.statSync(setupFilePath);
    console.log(`✅ Instalador principal encontrado: ${setupFile} (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
  } else {
    console.error('❌ Nenhum arquivo de setup (ex: "Dash Bird Setup X.Y.Z.exe") foi encontrado na pasta release.');
    allChecksPassed = false;
  }

  const unpackedExePath = path.join(releasePath, 'win-unpacked', 'Dash Bird.exe');
  if (fs.existsSync(unpackedExePath)) {
    const stats = fs.statSync(unpackedExePath);
    console.log(`✅ Executável descompactado encontrado: win-unpacked/Dash Bird.exe (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
  }
}

if (allChecksPassed) {
  console.log('\n✅ Todas as verificações passaram! Build está pronto.');
  process.exit(0);
} else {
  console.error('\n❌ Algumas verificações falharam. Verifique o build.');
  process.exit(1);
}
