import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

console.log('🧪 Testando caminhos para upload...');

// Caminhos
const projectRoot = path.join(__dirname, '..');
const releasePath = path.join(projectRoot, 'release');

console.log('📁 Diretório atual:', process.cwd());
console.log('📁 Project root:', projectRoot);
console.log('📁 Release path:', releasePath);

// Verificar se a pasta release existe
if (!fs.existsSync(releasePath)) {
  console.error('❌ Pasta release não encontrada!');
  process.exit(1);
}

// Listar conteúdo da pasta release
console.log('\n📁 Conteúdo da pasta release:');
const releaseItems = fs.readdirSync(releasePath);
releaseItems.forEach(item => {
  const itemPath = path.join(releasePath, item);
  const stats = fs.statSync(itemPath);
  
  if (stats.isDirectory()) {
    const files = fs.readdirSync(itemPath);
    console.log(`📁 ${item}/ - ${files.length} item(s)`);
  } else {
    console.log(`📄 ${item} (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
  }
});

// Verificar especificamente o Dash Bird.exe
console.log('\n🎯 Verificando Dash Bird.exe...');

// 1. Verificar na raiz
const dashBirdExeRoot = path.join(releasePath, 'Dash Bird.exe');
if (fs.existsSync(dashBirdExeRoot)) {
  const stats = fs.statSync(dashBirdExeRoot);
  console.log(`✅ Dash Bird.exe encontrado na raiz: ${dashBirdExeRoot}`);
  console.log(`   Tamanho: ${(stats.size / 1024 / 1024).toFixed(2)} MB`);
  console.log(`   Caminho relativo: Dash Bird.exe`);
} else {
  console.log('❌ Dash Bird.exe não encontrado na raiz');
}

// 2. Verificar em win-unpacked
const dashBirdExeWinUnpacked = path.join(releasePath, 'win-unpacked', 'Dash Bird.exe');
if (fs.existsSync(dashBirdExeWinUnpacked)) {
  const stats = fs.statSync(dashBirdExeWinUnpacked);
  console.log(`✅ Dash Bird.exe encontrado em win-unpacked: ${dashBirdExeWinUnpacked}`);
  console.log(`   Tamanho: ${(stats.size / 1024 / 1024).toFixed(2)} MB`);
  console.log(`   Caminho relativo: win-unpacked\\Dash Bird.exe`);
} else {
  console.log('❌ Dash Bird.exe não encontrado em win-unpacked');
}

// 3. Procurar em toda a pasta release
console.log('\n🔍 Procurando por todos os executáveis...');
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
        const relativePath = path.relative(releasePath, fullPath);
        executables.push({
          name: item,
          path: fullPath,
          relativePath: relativePath,
          size: stats.size
        });
      }
    });
  }
  
  scanDirectory(dirPath);
  return executables;
}

const allExecutables = findExecutables(releasePath);
if (allExecutables.length > 0) {
  console.log(`✅ Encontrados ${allExecutables.length} executável(is):`);
  allExecutables.forEach(exe => {
    const sizeMB = (exe.size / 1024 / 1024).toFixed(2);
    console.log(`  📄 ${exe.name} (${sizeMB} MB) - ${exe.relativePath}`);
  });
} else {
  console.log('❌ Nenhum executável encontrado');
}

// 4. Verificar se o arquivo pode ser acessado pelo caminho relativo
console.log('\n🔍 Testando acesso por caminho relativo...');
const workingDir = process.cwd();
const relativePath = 'Dash Bird.exe';
const fullPath = path.join(workingDir, relativePath);

console.log(`📁 Working directory: ${workingDir}`);
console.log(`📁 Caminho relativo: ${relativePath}`);
console.log(`📁 Caminho completo: ${fullPath}`);

if (fs.existsSync(fullPath)) {
  const stats = fs.statSync(fullPath);
  console.log(`✅ Arquivo acessível por caminho relativo: ${relativePath}`);
  console.log(`   Tamanho: ${(stats.size / 1024 / 1024).toFixed(2)} MB`);
} else {
  console.log(`❌ Arquivo NÃO acessível por caminho relativo: ${relativePath}`);
  
  // Tentar outros caminhos
  const alternativePaths = [
    'release/Dash Bird.exe',
    './Dash Bird.exe',
    'Dash Bird.exe'
  ];
  
  console.log('\n🔍 Tentando caminhos alternativos...');
  alternativePaths.forEach(altPath => {
    const altFullPath = path.join(workingDir, altPath);
    if (fs.existsSync(altFullPath)) {
      const stats = fs.statSync(altFullPath);
      console.log(`✅ Caminho alternativo funciona: ${altPath}`);
      console.log(`   Tamanho: ${(stats.size / 1024 / 1024).toFixed(2)} MB`);
    } else {
      console.log(`❌ Caminho alternativo não funciona: ${altPath}`);
    }
  });
}

console.log('\n✅ Teste concluído!');
