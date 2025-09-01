#!/usr/bin/env node

/**
 * 🧪 Script de Teste de Configuração de Build
 * Este script verifica se o ambiente está configurado corretamente para build
 */

import { existsSync, readFileSync } from 'fs';
import { join } from 'path';

console.log('🧪 Teste de Configuração de Build');
console.log('==================================');

// Verificar se estamos no diretório correto
const currentDir = process.cwd();
const isDesktopDir = existsSync(join(currentDir, 'package.json')) && 
                     existsSync(join(currentDir, 'electron-builder.yml'));

if (!isDesktopDir) {
    console.error('❌ Este script deve ser executado no diretório desktop/');
    process.exit(1);
}

// Função para verificar arquivo de configuração
function checkConfigFiles() {
    console.log('\n📁 Verificando arquivos de configuração...');
    
    const requiredFiles = [
        'package.json',
        'electron-builder.yml',
        'vite.config.ts',
        'tsconfig.json'
    ];
    
    let allFilesExist = true;
    
    for (const file of requiredFiles) {
        if (existsSync(file)) {
            console.log(`✅ ${file}`);
        } else {
            console.log(`❌ ${file} - NÃO ENCONTRADO`);
            allFilesExist = false;
        }
    }
    
    return allFilesExist;
}

// Função para verificar variáveis de ambiente
function checkEnvironmentVariables() {
    console.log('\n🔍 Verificando variáveis de ambiente...');
    
    const cscLink = process.env.CSC_LINK;
    const cscPassword = process.env.CSC_KEY_PASSWORD;
    const cscSha1 = process.env.CSC_KEY_SHA1;
    
    let hasSigning = true;
    
    if (!cscLink) {
        console.log('❌ CSC_LINK não configurado');
        hasSigning = false;
    } else {
        console.log(`✅ CSC_LINK: ${cscLink}`);
        
        // Verificar se é um arquivo ou string Base64
        if (existsSync(cscLink)) {
            console.log(`📋 Certificado encontrado como arquivo`);
        } else if (cscLink.length > 100) {
            console.log(`📋 Certificado detectado como string Base64 (${cscLink.length} caracteres)`);
        } else {
            console.log(`⚠️  CSC_LINK parece ser um caminho inválido`);
            hasSigning = false;
        }
    }
    
    if (!cscPassword) {
        console.log('❌ CSC_KEY_PASSWORD não configurado');
        hasSigning = false;
    } else {
        console.log('✅ CSC_KEY_PASSWORD: [CONFIGURADO]');
    }
    
    if (cscSha1) {
        console.log(`✅ CSC_KEY_SHA1: ${cscSha1}`);
    } else {
        console.log('ℹ️  CSC_KEY_SHA1 não configurado (opcional)');
    }
    
    return hasSigning;
}

// Função para verificar dependências
function checkDependencies() {
    console.log('\n📦 Verificando dependências...');
    
    try {
        const packageJson = JSON.parse(readFileSync('package.json', 'utf8'));
        
        const requiredDeps = [
            'electron',
            'electron-builder'
        ];
        
        let allDepsExist = true;
        
        for (const dep of requiredDeps) {
            if (packageJson.devDependencies?.[dep] || packageJson.dependencies?.[dep]) {
                console.log(`✅ ${dep}: ${packageJson.devDependencies?.[dep] || packageJson.dependencies?.[dep]}`);
            } else {
                console.log(`❌ ${dep} não encontrado`);
                allDepsExist = false;
            }
        }
        
        // Verificar scripts
        console.log('\n📜 Verificando scripts disponíveis...');
        const buildScripts = Object.keys(packageJson.scripts).filter(script => 
            script.includes('build') || script.includes('windows')
        );
        
        for (const script of buildScripts) {
            console.log(`📜 ${script}: ${packageJson.scripts[script]}`);
        }
        
        return allDepsExist;
        
    } catch (error) {
        console.error('❌ Erro ao ler package.json:', error.message);
        return false;
    }
}

// Função para verificar configuração do electron-builder
function checkElectronBuilderConfig() {
    console.log('\n⚙️  Verificando configuração do electron-builder...');
    
    try {
        const configContent = readFileSync('electron-builder.yml', 'utf8');
        
        // Verificar configurações importantes
        const checks = [
            { name: 'appId', pattern: /appId:/, required: true },
            { name: 'productName', pattern: /productName:/, required: true },
            { name: 'win.cscLink', pattern: /cscLink:/, required: false },
            { name: 'win.cscKeyPassword', pattern: /cscKeyPassword:/, required: false },
            { name: 'win.target', pattern: /target:/, required: true }
        ];
        
        let allChecksPass = true;
        
        for (const check of checks) {
            if (check.pattern.test(configContent)) {
                console.log(`✅ ${check.name}: Configurado`);
            } else if (check.required) {
                console.log(`❌ ${check.name}: NÃO CONFIGURADO (OBRIGATÓRIO)`);
                allChecksPass = false;
            } else {
                console.log(`ℹ️  ${check.name}: Não configurado (opcional)`);
            }
        }
        
        return allChecksPass;
        
    } catch (error) {
        console.error('❌ Erro ao ler electron-builder.yml:', error.message);
        return false;
    }
}

// Função principal
async function main() {
    try {
        console.log(`📍 Diretório atual: ${currentDir}`);
        
        // Verificações
        const configOk = checkConfigFiles();
        const envOk = checkEnvironmentVariables();
        const depsOk = checkDependencies();
        const electronBuilderOk = checkElectronBuilderConfig();
        
        // Resumo
        console.log('\n📊 RESUMO DAS VERIFICAÇÕES');
        console.log('============================');
        console.log(`📁 Arquivos de configuração: ${configOk ? '✅' : '❌'}`);
        console.log(`🔍 Variáveis de ambiente: ${envOk ? '✅' : '⚠️'}`);
        console.log(`📦 Dependências: ${depsOk ? '✅' : '❌'}`);
        console.log(`⚙️  Electron Builder: ${electronBuilderOk ? '✅' : '❌'}`);
        
        if (configOk && depsOk && electronBuilderOk) {
            if (envOk) {
                console.log('\n🎉 Ambiente configurado para build COM assinatura digital!');
                console.log('🚀 Execute: pnpm run build:windows:signed');
            } else {
                console.log('\n⚠️  Ambiente configurado para build SEM assinatura digital!');
                console.log('🚀 Execute: pnpm run build:windows');
                console.log('💡 Para habilitar assinatura, configure as variáveis CSC_LINK e CSC_KEY_PASSWORD');
            }
        } else {
            console.log('\n❌ Ambiente não configurado corretamente!');
            console.log('🔧 Corrija os problemas acima antes de prosseguir');
        }
        
    } catch (error) {
        console.error('\n❌ Erro durante a verificação:', error.message);
        process.exit(1);
    }
}

// Executar se chamado diretamente
if (import.meta.url === `file://${process.argv[1]}`) {
    main().catch(console.error);
}

export { main };
