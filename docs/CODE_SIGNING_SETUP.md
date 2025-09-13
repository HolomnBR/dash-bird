# 🔐 Configuração de Assinatura de Código para Dash Bird

Este documento explica como configurar a assinatura de código digital para o aplicativo Dash Bird no GitHub Actions.

## 📋 Pré-requisitos

1. **Certificado Digital (.pfx)** - Certificado de assinatura de código válido para Windows
2. **Senha do Certificado** - Senha para acessar o certificado
3. **Acesso ao Repositório** - Permissões para configurar secrets no GitHub

## 🚀 Passo a Passo

### 1. Preparar o Certificado

#### Opção A: Usando o Script Automatizado (Recomendado)

```powershell
# Execute o script de conversão
.\scripts\convert-certificate.ps1 -CertificatePath "caminho/para/seu/certificado.pfx" -Password "sua_senha"
```

O script irá:
- Converter o certificado para Base64
- Gerar o arquivo `certificate-base64.txt`
- Mostrar todas as informações necessárias

#### Opção B: Conversão Manual

```powershell
# PowerShell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("caminho/para/seu/certificado.pfx"))
```

```bash
# Bash/Linux
base64 -i caminho/para/seu/certificado.pfx
```

### 2. Configurar Secrets no GitHub

1. Acesse seu repositório no GitHub
2. Vá para **Settings** > **Secrets and variables** > **Actions**
3. Clique em **New repository secret**

#### Secrets Necessários:

| Nome do Secret | Valor | Descrição |
|----------------|-------|-----------|
| `CSC_LINK` | String Base64 do certificado | Certificado .pfx codificado em Base64 |
| `CSC_KEY_PASSWORD` | Senha do certificado | Senha para acessar o certificado |
| `CSC_KEY_SHA1` | Hash SHA1 (opcional) | Hash SHA1 do certificado para verificação |

### 3. Formato do Certificado

O `CSC_LINK` pode estar em dois formatos:

#### Formato 1: Base64 Puro
```
MIIK... (string Base64 longa)
```

#### Formato 2: Data URL (Recomendado)
```
data:application/x-pkcs12;base64,MIIK... (string Base64 longa)
```

## 🔧 Configuração do Workflow

O workflow já está configurado para:
1. Detectar automaticamente o formato do certificado
2. Decodificar o Base64 para arquivo .pfx
3. Configurar as variáveis de ambiente corretamente
4. Executar o build com assinatura digital

## 📁 Estrutura de Arquivos

```
desktop/
├── scripts/
│   ├── convert-certificate.ps1      # Conversor de certificado
│   ├── setup-code-signing.ps1       # Configurador de assinatura
│   └── verify-signature.ps1         # Verificador de assinatura
├── electron-builder.yml             # Configuração do electron-builder
└── private/                         # Diretório para certificados (criado automaticamente)
    └── certificate.pfx              # Certificado decodificado
```

## 🧪 Testando a Configuração

### Teste Local

```powershell
# Testar conversão de certificado
.\scripts\convert-certificate.ps1 -CertificatePath "teste.pfx" -Password "senha123"

# Testar configuração de assinatura
.\scripts\setup-code-signing.ps1 -CertificateBase64 "BASE64_DO_CERTIFICADO" -CertificatePassword "senha123"
```

### Teste no CI/CD

1. Faça push para a branch `main`
2. Verifique o workflow no GitHub Actions
3. Observe os logs para confirmar a assinatura

## 🚨 Solução de Problemas

### Erro: "Certificado não configurado"
- Verifique se os secrets estão configurados corretamente
- Confirme se os nomes dos secrets estão exatos
- Verifique se o valor do `CSC_LINK` é válido

### Erro: "Cannot resolve certificate path"
- O certificado não foi decodificado corretamente
- Verifique se o Base64 está correto
- Confirme se a senha está correta

### Erro: "Invalid certificate format"
- O certificado pode estar corrompido
- Tente reconverter o arquivo .pfx original
- Verifique se o certificado não expirou

## 📚 Recursos Adicionais

- [Documentação do electron-builder](https://www.electron.build/configuration/win)
- [Assinatura de Código no Windows](https://docs.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## 🔒 Segurança

⚠️ **IMPORTANTE:**
- Nunca commite certificados ou senhas no código
- Use sempre secrets do GitHub Actions
- Mantenha seus certificados seguros
- Revogue certificados comprometidos imediatamente

## 📞 Suporte

Se encontrar problemas:
1. Verifique os logs do GitHub Actions
2. Execute os scripts de teste localmente
3. Confirme a configuração dos secrets
4. Abra uma issue no repositório

---

**Última atualização:** $(Get-Date -Format "yyyy-MM-dd")
**Versão:** 1.0.0
