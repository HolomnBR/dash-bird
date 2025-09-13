# 🚀 Processo de Build do DashBird

## Visão Geral

Este documento explica como construir o aplicativo DashBird para Windows com suporte completo a assinatura digital, resolvendo o problema do Windows Defender SmartScreen.

## 🔐 Problema Resolvido

**Antes:** O Windows Defender SmartScreen bloqueava a execução do aplicativo com a mensagem:
> "O Microsoft Defender SmartScreen impediu que um aplicativo não reconhecido fosse iniciado."

**Depois:** Aplicativo assinado digitalmente, executando sem avisos de segurança.

## 📋 Scripts Disponíveis

### Build
```bash
# Build básico (sem assinatura)
pnpm run build:windows

# Build com assinatura digital (quando certificado configurado)
pnpm run build:windows:signed
```

### Verificação
```bash
# Verificar arquivos gerados
pnpm run verify:build

# Verificar assinatura digital
pnpm run verify:signature

# Verificar assinatura com detalhes
pnpm run verify:signature -- --Verbose
```

### Certificados
```bash
# Converter certificado para formato GitHub Actions
pnpm run convert:cert -- --CertificatePath "caminho/para/certificado.pfx" --Password "senha"
```

## 🏗️ Workflow de Build

### 1. Build Automático (GitHub Actions)
- ✅ Executa automaticamente em push para `main`
- ✅ Suporte a assinatura digital
- ✅ Verificação automática de assinatura
- ✅ Criação automática de releases

### 2. Build Local
```bash
# Instalar dependências
pnpm install

# Build completo
pnpm run build:windows

# Verificar resultado
pnpm run verify:signature
```

## 🔑 Configuração de Certificado

### Passo 1: Obter Certificado
- Comprar de uma Autoridade Certificadora (CA)
- Recomendado: EV Code Signing Certificate

### Passo 2: Converter Certificado
```bash
pnpm run convert:cert -- --CertificatePath "certificado.pfx" --Password "senha"
```

### Passo 3: Configurar GitHub Secrets
1. `CSC_LINK`: Certificado em Base64
2. `CSC_KEY_PASSWORD`: Senha do certificado
3. `CSC_KEY_SHA1`: Hash SHA1 (opcional)

## 📁 Estrutura de Arquivos

```
desktop/
├── electron-builder.yml          # ✅ Configuração de assinatura
├── package.json                  # ✅ Scripts de build
└── scripts/
    ├── convert-certificate.ps1   # ✅ Conversor de certificado
    └── verify-signature.ps1      # ✅ Verificador de assinatura

.github/
└── workflows/
    └── desktop-windows.yml      # ✅ Workflow com assinatura

# Documentação
├── CODE_SIGNING_SETUP.md        # ✅ Guia completo de assinatura
├── BUILD_PROCESS.md             # ✅ Este arquivo
└── README.md                    # ✅ Documentação principal
```

## 🔍 Verificação de Qualidade

### Durante o Build
- ✅ Verificação de dependências
- ✅ Compilação TypeScript
- ✅ Build Electron
- ✅ Assinatura digital (quando certificado disponível)

### Pós-Build
- ✅ Verificação de arquivos gerados
- ✅ Verificação de assinatura digital
- ✅ Validação de integridade

## 🚨 Troubleshooting

### Erro: "Certificate not found"
```bash
# Verificar se o certificado está configurado
echo $CSC_LINK

# Verificar se o secret está no GitHub
# Settings > Secrets > Actions > CSC_LINK
```

### Erro: "Invalid password"
```bash
# Verificar senha do certificado
echo $CSC_KEY_PASSWORD

# Testar certificado localmente
pnpm run convert:cert -- --CertificatePath "cert.pfx" --Password "senha"
```

### Erro: "Build failed"
```bash
# Verificar dependências
pnpm install --frozen-lockfile

# Verificar Node.js version
node --version  # Deve ser 20.x

# Verificar pnpm version
pnpm --version  # Deve ser 10.14.0
```

## 📊 Monitoramento

### Logs do Workflow
- Acesse: `Actions` tab no GitHub
- Verifique: `Build Desktop (Windows)`
- Status: ✅ Sucesso ou ❌ Falha

### Métricas de Build
- Tempo de execução
- Tamanho dos artefatos
- Status da assinatura
- Qualidade do código

## 🔄 Atualizações

### Renovação de Certificado
1. Obter novo certificado
2. Converter com `pnpm run convert:cert`
3. Atualizar secret `CSC_LINK`
4. Executar workflow

### Atualização de Dependências
```bash
# Atualizar pnpm
pnpm update

# Atualizar electron-builder
pnpm update electron-builder

# Verificar compatibilidade
pnpm run build:windows
```

## 📞 Suporte

### Documentação
- **Assinatura:** `CODE_SIGNING_SETUP.md`
- **Build:** `BUILD_PROCESS.md` (este arquivo)
- **Principal:** `README.md`

### Issues
- [GitHub Issues](https://github.com/HolomnBR/dash-bird/issues)
- Tag: `build`, `signing`, `windows`

### Comunidade
- **Discord:** [Link do servidor]
- **Email:** contato@holomn.com.br

---

**Nota:** Este processo garante que o DashBird seja distribuído de forma segura e profissional, sem avisos de segurança do Windows.
