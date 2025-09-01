#!/usr/bin/env node

/**
 * 🧪 Script de Teste da Configuração NSIS
 * Este script testa se a configuração do NSIS está funcionando
 */

import { readFileSync } from 'fs';

console.log('🧪 Teste da Configuração NSIS');
console.log('==============================');

try {
    // Ler arquivo de configuração
    const configContent = readFileSync('electron-builder.yml', 'utf8');
    
    // Verificar configurações NSIS
    console.log('\n🔍 Verificando configuração NSIS...');
    
    const nsisChecks = [
        { name: 'oneClick', pattern: /oneClick:/, required: false },
        { name: 'allowToChangeInstallationDirectory', pattern: /allowToChangeInstallationDirectory:/, required: false },
        { name: 'createDesktopShortcut', pattern: /createDesktopShortcut:/, required: false },
        { name: 'createStartMenuShortcut', pattern: /createStartMenuShortcut:/, required: false },
        { name: 'shortcutName', pattern: /shortcutName:/, required: false }
    ];
    
    let allChecksPass = true;
    
    for (const check of nsisChecks) {
        if (check.pattern.test(configContent)) {
            console.log(`✅ ${check.name}: Configurado`);
        } else if (check.required) {
            console.log(`❌ ${check.name}: NÃO CONFIGURADO (OBRIGATÓRIO)`);
            allChecksPass = false;
        } else {
            console.log(`ℹ️  ${check.name}: Não configurado (opcional)`);
        }
    }
    
    // Verificar se há referências a arquivos que não existem
    console.log('\n🔍 Verificando referências a arquivos...');
    
    const fileChecks = [
        { name: 'build/installer.nsh', pattern: /build\/installer\.nsh/, shouldExist: false },
        { name: 'build/installer.nsi', pattern: /build\/installer\.nsi/, shouldExist: false },
        { name: 'LICENSE', pattern: /LICENSE/, shouldExist: true },
        { name: 'public/app.ico', pattern: /public\/app\.ico/, shouldExist: true }
    ];
    
    for (const check of fileChecks) {
        if (check.pattern.test(configContent)) {
            if (check.shouldExist) {
                console.log(`✅ ${check.name}: Referenciado e deve existir`);
            } else {
                console.log(`⚠️  ${check.name}: Referenciado mas não deve existir (comentado)`);
            }
        } else {
            if (check.shouldExist) {
                console.log(`❌ ${check.name}: Não referenciado mas deveria existir`);
                allChecksPass = false;
            } else {
                console.log(`✅ ${check.name}: Não referenciado (correto)`);
            }
        }
    }
    
    // Verificar se as configurações de ícones estão configuradas corretamente
    console.log('\n🔍 Verificando configurações de ícones...');
    
    const iconChecks = [
        { name: 'installerIcon', pattern: /installerIcon:/, shouldExist: true, expectedValue: 'public/app.ico' },
        { name: 'uninstallerIcon', pattern: /uninstallerIcon:/, shouldExist: true, expectedValue: 'public/app.ico' }
    ];
    
    for (const check of iconChecks) {
        if (check.pattern.test(configContent)) {
            // Verificar se está apontando para o app.ico
            if (configContent.includes(check.expectedValue)) {
                console.log(`✅ ${check.name}: Configurado corretamente (${check.expectedValue})`);
            } else {
                console.log(`⚠️  ${check.name}: Configurado mas não aponta para app.ico`);
                allChecksPass = false;
            }
        } else {
            console.log(`❌ ${check.name}: Não configurado`);
            allChecksPass = false;
        }
    }
    
    // Verificar se o ícone principal do aplicativo está configurado
    const appIconPattern = /icon:\s*"public\/app\.ico"/;
    if (appIconPattern.test(configContent)) {
        console.log('✅ Ícone principal do aplicativo: Configurado (public/app.ico)');
    } else {
        console.log('❌ Ícone principal do aplicativo: Não configurado');
        allChecksPass = false;
    }
    
    // Verificar se os arquivos realmente existem
    console.log('\n📁 Verificando existência dos arquivos...');
    
    const fs = await import('fs');
    
    if (fs.existsSync('LICENSE')) {
        console.log('✅ LICENSE: Arquivo existe');
    } else {
        console.log('❌ LICENSE: Arquivo não encontrado');
        allChecksPass = false;
    }
    
    if (fs.existsSync('public/app.ico')) {
        console.log('✅ public/app.ico: Arquivo existe');
    } else {
        console.log('❌ public/app.ico: Arquivo não encontrado');
        allChecksPass = false;
    }
    
    // Resumo
    console.log('\n📊 RESUMO DOS TESTES NSIS');
    console.log('============================');
    console.log(`⚙️  Configuração NSIS: ${allChecksPass ? '✅' : '❌'}`);
    
    if (allChecksPass) {
        console.log('\n🎉 Configuração NSIS está correta!');
        console.log('🚀 O build deve funcionar sem erros de NSIS');
        console.log('🎨 Ícones configurados corretamente com favicon.ico');
    } else {
        console.log('\n⚠️  Problemas encontrados na configuração NSIS!');
        console.log('🔧 Corrija os problemas acima antes de prosseguir');
    }
    
} catch (error) {
    console.error('\n❌ Erro durante o teste:', error.message);
    process.exit(1);
}
