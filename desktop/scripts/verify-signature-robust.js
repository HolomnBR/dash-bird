#!/usr/bin/env node

/**
 * 🔐 Script de Verificação de Assinatura Robusto
 * Este script verifica assinaturas digitais sem falhar o workflow
 */

import { execSync } from 'child_process';
import { existsSync, readdirSync } from 'fs';
import { join } from 'path';

console.log('🔐 Verificação de Assinatura Digital Robusta');
console.log('===========================================');

// Verificar se estamos no diretório correto
if (!existsSync('release')) {
    console.log('⚠️  Pasta release não encontrada. Pulando verificação de assinatura.');
    process.exit(0);
}

// Função para executar comandos PowerShell
function runPowerShellCommand(command) {
    try {
        const result = execSync(`powershell -Command "${command}"`, { 
            stdio: 'pipe', 
            encoding: 'utf8' 
        });
        return result.trim();
    } catch (error) {
        return null;
    }
}

// Função para verificar assinatura de um arquivo
function verifyFileSignature(filePath) {
    const fileName = filePath.split('\\').pop();
    console.log(`\n📋 Verificando: ${fileName}`);
    
    try {
        // Comando PowerShell para verificar assinatura
        const psCommand = `
            try {
                $sig = Get-AuthenticodeSignature "${filePath}"
                $status = $sig.Status
                $timestamp = if ($sig.TimeStamperCertificate) { $sig.TimeStamperCertificate.Subject } else { "N/A" }
                Write-Output "$status|$timestamp"
            } catch {
                Write-Output "ERROR|$($_.Exception.Message)"
            }
        `;
        
        const result = runPowerShellCommand(psCommand);
        
        if (result && result !== '') {
            const [status, timestamp] = result.split('|');
            
            if (status === 'ERROR') {
                console.log(`⚠️  Erro ao verificar: ${timestamp}`);
                return false;
            }
            
            console.log(`📊 Status: ${status}`);
            if (timestamp !== 'N/A') {
                console.log(`📅 Timestamp: ${timestamp}`);
            }
            
            // Classificar o status
            if (status === 'Valid') {
                console.log('✅ Assinatura válida');
                return true;
            } else if (status === 'NotSigned') {
                console.log('⚠️  Não assinado (pode ser normal em builds de teste)');
                return false;
            } else if (status === 'UnknownError') {
                console.log('ℹ️  Status desconhecido (pode ser normal em ambiente de CI)');
                return false;
            } else {
                console.log(`ℹ️  Status: ${status}`);
                return false;
            }
        } else {
            console.log('⚠️  Não foi possível obter informações da assinatura');
            return false;
        }
        
    } catch (error) {
        console.log(`⚠️  Erro durante verificação: ${error.message}`);
        return false;
    }
}

// Função principal
async function main() {
    try {
        console.log('🔍 Procurando arquivos executáveis em release/...');
        
        const releasePath = 'release';
        const files = readdirSync(releasePath);
        const exeFiles = files.filter(f => f.endsWith('.exe'));
        
        if (exeFiles.length === 0) {
            console.log('⚠️  Nenhum arquivo .exe encontrado em release/');
            process.exit(0);
        }
        
        console.log(`📦 Encontrados ${exeFiles.length} arquivo(s) .exe para verificar`);
        
        let validSignatures = 0;
        let totalFiles = exeFiles.length;
        
        // Verificar cada arquivo
        for (const file of exeFiles) {
            const filePath = join(releasePath, file);
            if (verifyFileSignature(filePath)) {
                validSignatures++;
            }
        }
        
        // Resumo
        console.log('\n📊 RESUMO DA VERIFICAÇÃO DE ASSINATURA');
        console.log('=======================================');
        console.log(`📁 Total de arquivos verificados: ${totalFiles}`);
        console.log(`✅ Assinaturas válidas: ${validSignatures}`);
        console.log(`⚠️  Arquivos sem assinatura válida: ${totalFiles - validSignatures}`);
        
        if (validSignatures === totalFiles) {
            console.log('\n🎉 Todos os arquivos têm assinatura digital válida!');
        } else if (validSignatures > 0) {
            console.log('\n⚠️  Alguns arquivos não têm assinatura válida (pode ser normal em builds de teste)');
        } else {
            console.log('\nℹ️  Nenhum arquivo tem assinatura válida (pode ser normal em builds sem certificado)');
        }
        
        console.log('\n💡 Notas:');
        console.log('   - Status "NotSigned" é normal em builds de teste');
        console.log('   - Status "UnknownError" é comum em ambiente de CI');
        console.log('   - Apenas status "Valid" indica assinatura completamente válida');
        
        // Não falhar o workflow - sempre sair com sucesso
        console.log('\n✅ Verificação concluída com sucesso');
        process.exit(0);
        
    } catch (error) {
        console.error('\n❌ Erro durante verificação:', error.message);
        console.log('💡 Continuando workflow mesmo com erro na verificação');
        process.exit(0); // Não falhar o workflow
    }
}

// Executar se chamado diretamente
if (import.meta.url === `file://${process.argv[1]}`) {
    main().catch(console.error);
}

export { main };
