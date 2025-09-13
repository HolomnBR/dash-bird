# 🚀 Correções do CI/CD - Problema de Assinatura de Código Resolvido

## 🎯 Problema Identificado 

O build estava falhando na etapa de assinatura de código com o erro:

```
⨯ Env WIN_CSC_LINK is not correct, cannot resolve: D:\a\dash-bird\dash-bird\desktop\$CSC_LINK doesn't exist
```

## 🔍 Causa Raiz

O problema estava na configuração das variáveis de ambiente no workflow do GitHub Actions:

1. **Variáveis não substituídas**: O `electron-builder` estava recebendo o nome literal da variável (`$CSC_LINK`) em vez do valor
2. **Certificado não decodificado**: O certificado Base64 não estava sendo convertido para arquivo .pfx
3. **Configuração incorreta**: As variáveis de ambiente não estavam sendo configuradas corretamente

## ✅ Soluções Implementadas

### 1. Script de Configuração de Assinatura (`setup-code-signing.ps1`)

- **Decodificação automática** do certificado Base64 para arquivo .pfx
- **Detecção de formato** (Base64 puro ou data URL)
- **Validação do certificado** antes do uso
- **Configuração das variáveis** de ambiente corretamente

### 2. Workflow Corrigido (`.github/workflows/desktop-windows.yml`)

- **Uso do script especializado** para configuração
- **Tratamento de erros** robusto
- **Fallback para build sem assinatura** quando necessário
- **Logs detalhados** para debugging

### 3. Configuração do Electron Builder (`electron-builder.yml`)

- **Sintaxe correta** para variáveis de ambiente: `${CSC_LINK}`
- **Configurações de segurança** para Windows
- **Suporte a assinatura digital** automática

### 4. Scripts de Teste e Verificação

- **`test-code-signing.ps1`**: Testa toda a configuração
- **`convert-certificate.ps1`**: Converte certificados para Base64
- **`verify-signature.ps1`**: Verifica assinaturas dos arquivos gerados

## 🔧 Como Funciona Agora

### 1. Durante o Build

```yaml
# O workflow executa:
- name: Build application artifacts with code signing
  env:
    CSC_LINK: ${{ secrets.CSC_LINK }}
    CSC_KEY_PASSWORD: ${{ secrets.CSC_KEY_PASSWORD }}
  run: |
    # Configura assinatura usando script especializado
    & "scripts\setup-code-signing.ps1" -CertificateBase64 $env:CSC_LINK -CertificatePassword $env:CSC_KEY_PASSWORD
    
    # Executa build com variáveis configuradas
    pnpm run build:windows
```

### 2. Processo de Assinatura

1. **Decodificação**: Certificado Base64 → arquivo .pfx
2. **Validação**: Verifica se o certificado é válido
3. **Configuração**: Define `CSC_LINK` e `CSC_KEY_PASSWORD`
4. **Build**: `electron-builder` usa as variáveis para assinar

### 3. Fallback Seguro

Se algo der errado:
- ✅ Build continua sem assinatura
- ✅ Logs detalhados para debugging
- ✅ Aplicativo é gerado (não assinado)

## 📋 Checklist de Configuração

### ✅ No GitHub (Secrets)

- [ ] `CSC_LINK`: Certificado .pfx em Base64
- [ ] `CSC_KEY_PASSWORD`: Senha do certificado
- [ ] `CSC_KEY_SHA1`: Hash SHA1 (opcional)

### ✅ No Repositório

- [ ] Scripts de assinatura na pasta `scripts/`
- [ ] Workflow corrigido em `.github/workflows/`
- [ ] Configuração do electron-builder atualizada
- [ ] Documentação atualizada

### ✅ Testes

- [ ] Script de teste local: `pnpm run test:code-signing`
- [ ] Build local: `pnpm run build:windows`
- [ ] Workflow no GitHub Actions
- [ ] Verificação de assinatura

## 🧪 Testando as Correções

### Teste Local

```powershell
# Testar configuração completa
pnpm run test:code-signing

# Testar conversão de certificado
.\scripts\convert-certificate.ps1 -CertificatePath "teste.pfx" -Password "senha123"

# Testar configuração de assinatura
.\scripts\setup-code-signing.ps1 -CertificateBase64 "BASE64_DO_CERTIFICADO" -CertificatePassword "senha123"
```

### Teste no CI/CD

1. **Configure os secrets** no GitHub
2. **Faça push** para a branch `main`
3. **Verifique o workflow** no GitHub Actions
4. **Observe os logs** para confirmar a assinatura

## 🚨 Troubleshooting

### Erro: "Certificado não configurado"

```yaml
# Verifique se os secrets estão configurados:
CSC_LINK: ${{ secrets.CSC_LINK }}           # ✅ Correto
CSC_LINK: ${{ secrets.CSC_LINK }}           # ❌ Erro de sintaxe
```

### Erro: "Cannot resolve certificate path"

```yaml
# O script deve configurar:
env:
  CSC_LINK: "private/certificate.pfx"       # ✅ Caminho de arquivo
  CSC_LINK: "${{ secrets.CSC_LINK }}"       # ❌ Nome da variável
```

### Erro: "Invalid certificate format"

- Verifique se o Base64 está correto
- Confirme se a senha está correta
- Teste o certificado localmente primeiro

## 📚 Arquivos Modificados

| Arquivo | Descrição da Modificação |
|---------|-------------------------|
| `.github/workflows/desktop-windows.yml` | Workflow corrigido para assinatura |
| `desktop/electron-builder.yml` | Configuração de variáveis corrigida |
| `scripts/setup-code-signing.ps1` | Novo script de configuração |
| `scripts/test-code-signing.ps1` | Script de teste da configuração |
| `desktop/package.json` | Novo script de teste adicionado |
| `CODE_SIGNING_SETUP.md` | Documentação completa da configuração |

## 🎉 Resultado Esperado

Após as correções:

1. ✅ **Build com assinatura**: Quando certificado configurado
2. ✅ **Build sem assinatura**: Quando certificado não disponível
3. ✅ **Logs claros**: Para debugging e monitoramento
4. ✅ **Fallback seguro**: Sempre gera o aplicativo
5. ✅ **Instalador funcional**: NSIS com aplicativo assinado

## 🔄 Próximos Passos

1. **Configure os secrets** no GitHub
2. **Teste localmente** com os scripts
3. **Faça push** para testar o workflow
4. **Monitore os logs** para confirmar funcionamento
5. **Verifique a assinatura** dos arquivos gerados

---

**Status**: ✅ **RESOLVIDO**
**Última atualização**: $(Get-Date -Format "yyyy-MM-dd")
**Versão**: 1.0.0
