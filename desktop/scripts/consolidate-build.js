import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

console.log('📁 Consolidando artefatos de build na pasta release...');

// Caminhos
const projectRoot = path.join(__dirname, '..');
const releasePath = path.join(projectRoot, 'release');
const apiDistPath = path.join(projectRoot, 'api-dist');
const distPath = path.join(projectRoot, 'dist');
const distElectronPath = path.join(projectRoot, 'dist-electron');

// Função para copiar arquivos recursivamente
function copyRecursive(src, dest) {
  if (!fs.existsSync(src)) {
    console.log(`⚠️  Origem não encontrada: ${src}`);
    return;
  }

  const stats = fs.statSync(src);
  
  if (stats.isDirectory()) {
    if (!fs.existsSync(dest)) {
      fs.mkdirSync(dest, { recursive: true });
    }
    
    const files = fs.readdirSync(src);
    files.forEach(file => {
      const srcPath = path.join(src, file);
      const destPath = path.join(dest, file);
      copyRecursive(srcPath, destPath);
    });
  } else {
    // Criar diretório de destino se não existir
    const destDir = path.dirname(dest);
    if (!fs.existsSync(destDir)) {
      fs.mkdirSync(destDir, { recursive: true });
    }
    
    fs.copyFileSync(src, dest);
  }
}

// Função para listar arquivos em uma pasta
function listFiles(dirPath, prefix = '') {
  if (!fs.existsSync(dirPath)) return;
  
  const items = fs.readdirSync(dirPath);
  items.forEach(item => {
    const fullPath = path.join(dirPath, item);
    const stats = fs.statSync(fullPath);
    const size = stats.isFile() ? ` (${(stats.size / 1024).toFixed(2)} KB)` : '/';
    console.log(`${prefix}${stats.isFile() ? '📄' : '📁'} ${item}${size}`);
  });
}

// Função para encontrar executáveis
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

try {
  // Garantir que a pasta release existe
  if (!fs.existsSync(releasePath)) {
    fs.mkdirSync(releasePath, { recursive: true });
    console.log('✅ Pasta release criada');
  } else {
    console.log('✅ Pasta release já existe');
  }

  // 1. Copiar arquivos da API
  if (fs.existsSync(apiDistPath)) {
    console.log('\n📦 Copiando arquivos da API...');
    const apiDestPath = path.join(releasePath, 'api-dist');
    copyRecursive(apiDistPath, apiDestPath);
    console.log('✅ Arquivos da API copiados para release/api-dist');
    
    // Listar arquivos copiados
    console.log('📁 Conteúdo de release/api-dist:');
    listFiles(apiDestPath, '  ');
  } else {
    console.log('⚠️  Pasta api-dist não encontrada');
  }

  // 2. Copiar arquivos do build do Electron (Vite)
  if (fs.existsSync(distPath)) {
    console.log('\n📦 Copiando arquivos do build do Electron...');
    const distDestPath = path.join(releasePath, 'dist');
    copyRecursive(distPath, distDestPath);
    console.log('✅ Arquivos do Electron copiados para release/dist');
    
    // Listar arquivos copiados
    console.log('📁 Conteúdo de release/dist:');
    listFiles(distDestPath, '  ');
  } else {
    console.log('⚠️  Pasta dist não encontrada');
  }

  // 3. Copiar arquivos do dist-electron
  if (fs.existsSync(distElectronPath)) {
    console.log('\n📦 Copiando arquivos do dist-electron...');
    const distElectronDestPath = path.join(releasePath, 'dist-electron');
    copyRecursive(distElectronPath, distElectronDestPath);
    console.log('✅ Arquivos do dist-electron copiados para release/dist-electron');
    
    // Listar arquivos copiados
    console.log('📁 Conteúdo de release/dist-electron:');
    listFiles(distElectronDestPath, '  ');
  } else {
    console.log('⚠️  Pasta dist-electron não encontrada');
  }

  // 4. Verificar se há executáveis do electron-builder
  const winUnpackedPath = path.join(releasePath, 'win-unpacked');
  if (fs.existsSync(winUnpackedPath)) {
    console.log('\n📦 Verificando executáveis do win-unpacked...');
    const exeFiles = fs.readdirSync(winUnpackedPath).filter(f => f.endsWith('.exe'));
    
    if (exeFiles.length > 0) {
      console.log(`✅ Encontrados ${exeFiles.length} executável(is) em win-unpacked:`);
      exeFiles.forEach(exe => {
        const exePath = path.join(winUnpackedPath, exe);
        const stats = fs.statSync(exePath);
        console.log(`  📄 ${exe} (${(stats.size / 1024 / 1024).toFixed(2)} MB)`);
      });
    } else {
      console.log('⚠️  Nenhum executável encontrado em win-unpacked');
    }
  }

  // 5. Procurar por executáveis em toda a pasta release
  console.log('\n🔍 Procurando por executáveis em toda a pasta release...');
  const allExecutables = findExecutables(releasePath);
  
  if (allExecutables.length > 0) {
    console.log(`✅ Encontrados ${allExecutables.length} executável(is):`);
    allExecutables.forEach(exe => {
      const sizeMB = (exe.size / 1024 / 1024).toFixed(2);
      const relativePath = path.relative(releasePath, exe.path);
      console.log(`  📄 ${exe.name} (${sizeMB} MB) - ${relativePath}`);
    });
    
    // Verificar especificamente o Dash Bird.exe
    const dashBirdExe = allExecutables.find(exe => exe.name === 'Dash Bird.exe');
    if (dashBirdExe) {
      console.log(`\n🎯 Dash Bird.exe encontrado: ${dashBirdExe.path}`);
      console.log(`   Tamanho: ${(dashBirdExe.size / 1024 / 1024).toFixed(2)} MB`);
      
      // Se estiver em win-unpacked, copiar para a raiz da pasta release
      if (dashBirdExe.path.includes('win-unpacked')) {
        const targetPath = path.join(releasePath, 'Dash Bird.exe');
        fs.copyFileSync(dashBirdExe.path, targetPath);
        console.log('✅ Dash Bird.exe copiado para a raiz da pasta release');
      }
    } else {
      console.log('⚠️  Dash Bird.exe não encontrado');
    }
  } else {
    console.log('❌ Nenhum executável encontrado na pasta release');
  }

  // 6. Resumo final
  console.log('\n📋 Resumo da consolidação:');
  const releaseItems = fs.readdirSync(releasePath);
  
  releaseItems.forEach(item => {
    const itemPath = path.join(releasePath, item);
    const stats = fs.statSync(itemPath);
    
    if (stats.isDirectory()) {
      const files = fs.readdirSync(itemPath);
      console.log(`📁 ${item}/ - ${files.length} item(s)`);
    } else {
      console.log(`📄 ${item} (${(stats.size / 1024).toFixed(2)} KB)`);
    }
  });

  console.log('\n✅ Consolidação de artefatos concluída com sucesso!');
  console.log(`📁 Todos os arquivos estão em: ${releasePath}`);

} catch (error) {
  console.error('❌ Erro durante a consolidação:', error.message);
  process.exit(1);
}
