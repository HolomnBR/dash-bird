# 🔐 Configuração de Assinatura de Código para Windows

## Problema Resolvido

Este documento explica como configurar a assinatura digital do aplicativo DashBird para Windows, resolvendo o erro:

> **"O Microsoft Defender SmartScreen impediu que um aplicativo não reconhecido fosse iniciado. A execução deste aplicativo pode colocar o computador em risco."**

## ✅ Solução Implementada

### 1. Configuração do Electron Builder
- ✅ Configuração de certificado digital no `electron-builder.yml`
- ✅ Suporte a assinatura automática durante o build
- ✅ Configurações de segurança para Windows

### 2. Workflow GitHub Actions Atualizado
- ✅ Variáveis de ambiente para certificados
- ✅ Verificação automática de assinatura
- ✅ Build com assinatura digital

## 🔑 Passos para Configurar

### Passo 1: Obter Certificado Digital

1. **Comprar um certificado de uma Autoridade Certificadora (CA):**
   - [DigiCert](https://www.digicert.com/)
   - [Sectigo](https://sectigo.com/)
   - [GlobalSign](https://www.globalsign.com/)
   - [Comodo](https://www.comodo.com/)

2. **Tipos de certificado recomendados:**
   - **Code Signing Certificate** (básico)
   - **EV Code Signing Certificate** (recomendado - mais confiável)

### Passo 2: Converter Certificado para Formato .pfx

1. **Se você recebeu um arquivo .crt ou .pem:**
   ```bash
   # Converter para .pfx
   openssl pkcs12 -export -out certificate.pfx -inkey private.key -in certificate.crt
   ```

2. **Se você recebeu um arquivo .pfx diretamente:**
   - Use o arquivo como está

### Passo 3: Codificar em Base64

1. **Converter o arquivo .pfx para Base64:**
   ```bash
   # Windows PowerShell
   $certBytes = Get-Content "certificate.pfx" -Encoding Byte
   $base64 = [Convert]::ToBase64String($certBytes)
   $dataUrl = "data:application/x-pkcs12;base64,$base64"
   echo $dataUrl
   ```

2. **Ou usar ferramentas online:**
   - [Base64 Encode](https://www.base64encode.org/)
   - [Convert Files to Base64](https://base64.guru/converter/encode/file)

### Passo 4: Configurar Secrets no GitHub

1. **Acesse seu repositório no GitHub**
2. **Vá para Settings > Secrets and variables > Actions**
3. **Adicione os seguintes secrets:**

   | Secret Name | Valor | Descrição |
   |-------------|-------|-----------|
   | `CSC_LINK` | `data:application/x-pkcs12;base64,MIIJ...` | Certificado .pfx em Base64 |
   | `CSC_KEY_PASSWORD` | `sua_senha_aqui` | Senha do certificado |
   | `CSC_KEY_SHA1` | `A1B2C3D4E5F6...` | Hash SHA1 do certificado (opcional) |

### Passo 5: Verificar Configuração

1. **Commit e push das alterações**
2. **Verificar se o workflow executa com sucesso**
3. **Verificar se os arquivos .exe estão assinados**

## 🔍 Verificação da Assinatura

### Durante o Build
O workflow verifica automaticamente:
- ✅ Presença do certificado
- ✅ Assinatura dos arquivos .exe
- ✅ Status da assinatura digital

### Verificação Manual
```powershell
# Verificar assinatura de um arquivo .exe
Get-AuthenticodeSignature "DashBird.exe"
```

## 📋 Estrutura de Arquivos Atualizada

```
desktop/
├── electron-builder.yml          # ✅ Configuração de assinatura
├── package.json                  # ✅ Scripts de build
└── ...

.github/
└── workflows/
    └── desktop-windows.yml      # ✅ Workflow com assinatura
```

## 🚀 Benefícios da Assinatura

1. **Segurança:**
   - ✅ Windows Defender SmartScreen não bloqueia
   - ✅ Aplicativo reconhecido como confiável
   - ✅ Execução sem avisos de segurança

2. **Profissionalismo:**
   - ✅ Marca da empresa visível
   - ✅ Confiança dos usuários
   - ✅ Distribuição corporativa facilitada

3. **Compatibilidade:**
   - ✅ Windows 10/11
   - ✅ Windows Server
   - ✅ Sistemas corporativos

## ⚠️ Troubleshooting

### Erro: "Certificate not found"
- Verifique se `CSC_LINK` está configurado corretamente
- Certifique-se de que o certificado é válido

### Erro: "Invalid password"
- Verifique se `CSC_KEY_PASSWORD` está correto
- Teste a senha localmente primeiro

### Erro: "Certificate expired"
- Renove o certificado antes da expiração
- Atualize o secret `CSC_LINK`

## 💰 Custos Estimados

| Tipo de Certificado | Preço Anual | Validade |
|---------------------|-------------|----------|
| Code Signing | $99 - $299 | 1-3 anos |
| EV Code Signing | $299 - $599 | 1-3 anos |

## 📞 Suporte

- **Documentação:** Este arquivo
- **Issues:** [GitHub Issues](https://github.com/HolomnBR/dash-bird/issues)
- **Workflow:** `.github/workflows/desktop-windows.yml`

---

**Nota:** A assinatura de código é obrigatória para distribuição profissional de software Windows. Sem ela, os usuários enfrentarão constantes avisos de segurança.
