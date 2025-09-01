#!/usr/bin/env node

/**
 * 📦 Script de Build Apenas da Versão Portátil
 * Este script evita problemas com NSIS fazendo build apenas da versão portátil
 */

import { execSync } from 'child_process';
import { existsSync } from 'fs';
import { join } from 'path';

console.log('📦 Script de Build - Versão Portátil Apenas');
console.log('============================================');

// Verificar se estamos no diretório correto
const currentDir = process.cwd();
const isDesktopDir = existsSync(join(currentDir, 'package.json')) && 
                     existsSync(join(currentDir, 'electron-builder.yml'));

if (!isDesktopDir) {
    console.error('❌ Este script deve ser executado no diretório desktop/');
    process.exit(1);
}

// Função para executar comandos
function runCommand(command, options = {}) {
    try {
        console.log(`🚀 Executando: ${command}`);
        const result = execSync(command, { 
            stdio: 'inherit', 
            encoding: 'utf8',
            ...options
        });
        return result;
    } catch (error) {
        console.error(`❌ Erro ao executar: ${command}`);
        console.error(`Erro: ${error.message}`);
        process.exit(1);
    }
}

// Função para verificar variáveis de ambiente
function checkEnvironment() {
    console.log('\n🔍 Verificando variáveis de ambiente...');
    
    const cscLink = process.env.CSC_LINK;
    const cscPassword = process.env.CSC_KEY_PASSWORD;
    
    if (!cscLink) {
        console.warn('⚠️  CSC_LINK não configurado - build sem assinatura digital');
        return false;
    }
    
    if (!cscPassword) {
        console.warn('⚠️  CSC_KEY_PASSWORD não configurado - build sem assinatura digital');
        return false;
    }
    
    // Verificar se o arquivo do certificado existe
    if (!existsSync(cscLink)) {
        console.error(`❌ Certificado não encontrado em: ${cscLink}`);
        console.error('💡 Certifique-se de que o certificado foi configurado corretamente');
        return false;
    }
    
    console.log('✅ Variáveis de ambiente configuradas corretamente');
    console.log(`📋 Certificado: ${cscLink}`);
    console.log(`🔑 Senha: [CONFIGURADA]`);
    
    return true;
}

// Função principal
async function main() {
    try {
        // Verificar ambiente
        const hasSigning = checkEnvironment();
        
        if (hasSigning) {
            console.log('\n🔐 Iniciando build COM assinatura digital (portátil apenas)...');
        } else {
            console.log('\n⚠️  Iniciando build SEM assinatura digital (portátil apenas)...');
        }
        
        // Build da API
        console.log('\n📦 Build da API...');
        runCommand('pnpm run build:api');
        
        // Build do Electron
        console.log('\n⚡ Build do Electron...');
        runCommand('pnpm run build:electron');
        
        // Build final APENAS da versão portátil
        console.log('\n🚀 Build final da versão portátil...');
        if (hasSigning) {
            console.log('🔐 Usando configuração de assinatura digital...');
            // Build apenas da versão portátil para evitar problemas com NSIS
            runCommand('electron-builder --win portable --publish never');
        } else {
            console.log('⚠️  Build sem assinatura digital...');
            runCommand('electron-builder --win portable --publish never');
        }
        
        console.log('\n✅ Build da versão portátil concluído com sucesso!');
        
        // Verificar arquivos gerados
        console.log('\n📁 Verificando arquivos gerados...');
        runCommand('node scripts/verify-build.js');
        
        console.log('\n💡 Nota: Este build criou apenas a versão portátil (.exe)');
        console.log('💡 Para criar o instalador NSIS, use: pnpm run build:windows:signed');
        
    } catch (error) {
        console.error('\n❌ Erro durante o build:', error.message);
        process.exit(1);
    }
}

// Executar se chamado diretamente
if (import.meta.url === `file://${process.argv[1]}`) {
    main().catch(console.error);
}

export { main };
