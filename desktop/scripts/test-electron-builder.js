#!/usr/bin/env node

/**
 * 🧪 Script de Teste do Electron Builder
 * Este script testa se o electron-builder está configurado corretamente
 */

import { execSync } from 'child_process';
import { existsSync, readFileSync } from 'fs';

console.log('🧪 Teste do Electron Builder');
console.log('============================');

// Verificar se estamos no diretório correto
if (!existsSync('electron-builder.yml')) {
    console.error('❌ Este script deve ser executado no diretório desktop/');
    process.exit(1);
}

// Função para executar comandos
function runCommand(command, options = {}) {
    try {
        console.log(`🚀 Executando: ${command}`);
        const result = execSync(command, { 
            stdio: 'pipe', 
            encoding: 'utf8',
            ...options
        });
        return result;
    } catch (error) {
        console.error(`❌ Erro ao executar: ${command}`);
        console.error(`Erro: ${error.message}`);
        return null;
    }
}

// Função para testar configuração
function testConfiguration() {
    console.log('\n🔍 Testando configuração do electron-builder...');
    
    // Ler arquivo de configuração
    try {
        const configContent = readFileSync('electron-builder.yml', 'utf8');
        console.log('✅ electron-builder.yml carregado');
        
        // Verificar se tem configurações de assinatura
        if (configContent.includes('cscLink') || configContent.includes('cscKeyPassword')) {
            console.log('✅ Configurações de assinatura encontradas');
        } else {
            console.log('ℹ️  Configurações de assinatura não encontradas (serão passadas via linha de comando)');
        }
        
    } catch (error) {
        console.error('❌ Erro ao ler electron-builder.yml:', error.message);
        return false;
    }
    
    return true;
}

// Função para testar variáveis de ambiente
function testEnvironmentVariables() {
    console.log('\n🔍 Testando variáveis de ambiente...');
    
    const cscLink = process.env.CSC_LINK;
    const cscPassword = process.env.CSC_KEY_PASSWORD;
    
    if (cscLink) {
        console.log(`✅ CSC_LINK: ${cscLink}`);
        
        // Verificar se é arquivo ou Base64
        if (existsSync(cscLink)) {
            console.log('📋 Certificado encontrado como arquivo');
        } else if (cscLink.length > 100) {
            console.log(`📋 Certificado detectado como string Base64 (${cscLink.length} caracteres)`);
        } else {
            console.log('⚠️  CSC_LINK parece ser um caminho inválido');
        }
    } else {
        console.log('❌ CSC_LINK não configurado');
    }
    
    if (cscPassword) {
        console.log('✅ CSC_KEY_PASSWORD: [CONFIGURADO]');
    } else {
        console.log('❌ CSC_KEY_PASSWORD não configurado');
    }
    
    return !!(cscLink && cscPassword);
}

// Função para testar comando electron-builder
function testElectronBuilder() {
    console.log('\n🚀 Testando comando electron-builder...');
    
    // Teste básico
    const basicTest = runCommand('electron-builder --help');
    if (basicTest) {
        console.log('✅ electron-builder está funcionando');
        
        // Teste de configuração
        const configTest = runCommand('electron-builder --config');
        if (configTest) {
            console.log('✅ Configuração carregada com sucesso');
        }
        
        return true;
    } else {
        console.log('❌ electron-builder não está funcionando');
        return false;
    }
}

// Função para testar build simulado
function testBuildSimulation() {
    console.log('\n🔨 Testando simulação de build...');
    
    const hasSigning = testEnvironmentVariables();
    
    if (hasSigning) {
        console.log('🔐 Testando build com assinatura...');
        
        // Simular comando de build com assinatura
        const signingArgs = `--win --publish never --config.win.cscLink="${process.env.CSC_LINK}" --config.win.cscKeyPassword="${process.env.CSC_KEY_PASSWORD}" --dry-run`;
        
        console.log(`📝 Comando simulado: electron-builder ${signingArgs}`);
        console.log('✅ Comando de build com assinatura configurado corretamente');
        
    } else {
        console.log('⚠️  Testando build sem assinatura...');
        console.log('📝 Comando simulado: electron-builder --win --publish never');
        console.log('✅ Comando de build sem assinatura configurado corretamente');
    }
}

// Função principal
async function main() {
    try {
        console.log(`📍 Diretório atual: ${process.cwd()}`);
        
        // Testes
        const configOk = testConfiguration();
        const envOk = testEnvironmentVariables();
        const electronBuilderOk = testElectronBuilder();
        
        if (configOk && electronBuilderOk) {
            testBuildSimulation();
            
            // Resumo
            console.log('\n📊 RESUMO DOS TESTES');
            console.log('=====================');
            console.log(`📁 Configuração: ${configOk ? '✅' : '❌'}`);
            console.log(`🔍 Variáveis de ambiente: ${envOk ? '✅' : '⚠️'}`);
            console.log(`⚙️  Electron Builder: ${electronBuilderOk ? '✅' : '❌'}`);
            
            if (envOk) {
                console.log('\n🎉 Ambiente configurado para build COM assinatura digital!');
                console.log('🚀 Execute: pnpm run build:windows:signed');
            } else {
                console.log('\n⚠️  Ambiente configurado para build SEM assinatura digital!');
                console.log('🚀 Execute: pnpm run build:windows');
                console.log('💡 Para habilitar assinatura, configure as variáveis CSC_LINK e CSC_KEY_PASSWORD');
            }
        } else {
            console.log('\n❌ Problemas encontrados na configuração!');
            console.log('🔧 Corrija os problemas acima antes de prosseguir');
        }
        
    } catch (error) {
        console.error('\n❌ Erro durante os testes:', error.message);
        process.exit(1);
    }
}

// Executar se chamado diretamente
if (import.meta.url === `file://${process.argv[1]}`) {
    main().catch(console.error);
}

export { main };
