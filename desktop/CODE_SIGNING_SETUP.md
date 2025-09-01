# 🔐 Configuração de Assinatura de Código para Dash Bird

Este documento explica como configurar a assinatura de código digital para o aplicativo Dash Bird no Windows.

## 📋 Pré-requisitos

- Certificado de assinatura de código válido (.pfx)
- Senha do certificado
- Windows 10/11 ou Windows Server 2016+
- Node.js 20+ e pnpm instalados

## 🚀 Configuração Local

### 1. Preparar o Certificado

#### Opção A: Arquivo .pfx local
```bash
# Copie seu certificado para o diretório desktop/
cp caminho/para/certificado.pfx desktop/private/certificate.pfx
```

#### Opção B: Converter para Base64
```powershell
# No PowerShell, converta o certificado para Base64
[Convert]::ToBase64String([IO.File]::ReadAllBytes("caminho\para\certificado.pfx"))
```

### 2. Configurar Variáveis de Ambiente

#### Windows (PowerShell)
```powershell
# Para arquivo local
$env:CSC_LINK = "private\certificate.pfx"
$env:CSC_KEY_PASSWORD = "sua_senha_aqui"

# Para Base64
$env:CSC_LINK = "MIIJ...sua_string_base64_aqui"
$env:CSC_KEY_PASSWORD = "sua_senha_aqui"
```

#### Windows (CMD)
```cmd
set CSC_LINK=private\certificate.pfx
set CSC_KEY_PASSWORD=sua_senha_aqui
```

#### Linux/macOS
```bash
export CSC_LINK="private/certificate.pfx"
export CSC_KEY_PASSWORD="sua_senha_aqui"
```

### 3. Testar a Configuração

```bash
cd desktop
pnpm run test:build-config
```

### 4. Executar Build com Assinatura

```bash
# Build com assinatura digital
pnpm run build:windows:signed

# Build sem assinatura (fallback)
pnpm run build:windows
```

## 🔧 Configuração no GitHub Actions

### 1. Adicionar Secrets

No seu repositório GitHub, vá para **Settings** → **Secrets and variables** → **Actions** e adicione:

- `CSC_LINK`: String Base64 do seu certificado
- `CSC_KEY_PASSWORD`: Senha do certificado
- `CSC_KEY_SHA1`: Hash SHA1 do certificado (opcional)

### 2. Converter Certificado para Base64

```powershell
# No PowerShell local
$certBytes = [IO.File]::ReadAllBytes("caminho\para\certificado.pfx")
$base64 = [Convert]::ToBase64String($certBytes)
$base64 | Set-Clipboard
```

### 3. Workflow Automático

O workflow `.github/workflows/desktop-windows.yml` já está configurado para:
- Detectar automaticamente se o certificado está disponível
- Configurar o ambiente de assinatura
- Executar build com ou sem assinatura conforme disponibilidade

## 🧪 Scripts de Teste

### Verificar Configuração
```bash
pnpm run test:build-config
```

### Testar Assinatura
```bash
pnpm run test:code-signing
```

### Verificar Build
```bash
pnpm run verify:build
```

### Verificar Assinatura dos Arquivos
```bash
pnpm run verify:signature
```

## 📁 Estrutura de Arquivos

```
desktop/
├── scripts/
│   ├── build-with-signing.js      # Script principal de build
│   ├── test-build-config.js       # Teste de configuração
│   └── verify-build.js            # Verificação de build
├── private/                        # Diretório para certificados (criado automaticamente)
├── electron-builder.yml            # Configuração do electron-builder
├── package.json                    # Scripts e dependências
└── env.example                     # Exemplo de variáveis de ambiente
```

## 🔍 Solução de Problemas

### Erro: "CSC_LINK is not correct"
- Verifique se a variável `CSC_LINK` está configurada
- Certifique-se de que o caminho do arquivo está correto
- Para Base64, verifique se a string está completa

### Erro: "Certificate password is incorrect"
- Verifique se `CSC_KEY_PASSWORD` está configurada corretamente
- Teste a senha abrindo o certificado no Windows

### Build sem assinatura
- Se as variáveis não estiverem configuradas, o build prosseguirá sem assinatura
- Isso é normal para desenvolvimento local

### Verificar certificado
```powershell
# No PowerShell
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("caminho\para\certificado.pfx", "senha")
$cert.Subject
$cert.NotAfter
```

## 📚 Recursos Adicionais

- [Documentação do electron-builder](https://www.electron.build/)
- [Assinatura de código no Windows](https://docs.microsoft.com/en-us/windows/win32/seccrypto/code-signing)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)

## 🆘 Suporte

Se encontrar problemas:
1. Execute `pnpm run test:build-config` para diagnóstico
2. Verifique os logs do GitHub Actions
3. Consulte a documentação do electron-builder
4. Abra uma issue no repositório
