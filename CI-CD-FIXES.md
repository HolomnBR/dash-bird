# Correções do Pipeline CI/CD

## Problema Identificado

O job "build-windows" estava falhando com o erro:
```
Error: Unable to locate executable file: pnpm. Please verify either the file path exists or the file can be found within the directory specified by the PATH environment variable.
```

## Causa Raiz

A ordem dos passos no workflow estava incorreta:
1. ❌ `actions/setup-node@v4` estava sendo executado antes de `pnpm/action-setup@v4`
2. ❌ O cache do pnpm estava sendo configurado antes do pnpm estar disponível
3. ❌ Faltava o setup do .NET SDK para compilar a API

## Correções Implementadas

### 1. Ordem Correta dos Passos
```yaml
- name: Setup pnpm          # Primeiro: instalar pnpm
- name: Setup Node          # Segundo: configurar Node com cache pnpm
- name: Setup .NET          # Terceiro: instalar .NET SDK
```

### 2. Configuração de Cache Melhorada
- Adicionado cache específico para pnpm store
- Configuração correta do cache no setup do Node

### 3. Setup do .NET
- Adicionado `actions/setup-dotnet@v4` para compilar a API .NET Core

### 4. Arquivos Modificados
- `.github/workflows/desktop-windows.yml` - Workflow principal corrigido
- `.github/workflows/test-build.yml` - Workflow de teste criado
- `.npmrc` - Configuração do pnpm adicionada

## Como Testar

1. **Workflow de Teste**: Execute o workflow "Test Build" manualmente para verificar se as correções funcionam
2. **Build Completo**: Faça push para a branch `main` para executar o build completo

## Estrutura do Build

O build agora segue esta sequência:
1. Setup do ambiente (.NET, pnpm, Node)
2. Instalação de dependências
3. Build da API .NET Core
4. Build do Electron
5. Build dos artefatos Windows
6. Upload dos artefatos

## Verificações Adicionais

- ✅ Ordem correta dos passos
- ✅ Cache configurado adequadamente
- ✅ .NET SDK disponível para build da API
- ✅ Scripts de build separados para melhor debugging
