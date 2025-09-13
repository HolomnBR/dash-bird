# 🧹 Guia de Limpeza Completa - Mudança de Branch

Este guia explica como limpar completamente o projeto Dash Bird ao mudar de branch, garantindo que não haja cache ou builds antigos interferindo no desenvolvimento.

## 🎯 Quando Usar

Use este processo quando:
- ✅ Mudar de branch (especialmente entre branches com mudanças significativas)
- ✅ Encontrar problemas de build inexplicáveis
- ✅ Dependências desatualizadas ou corrompidas
- ✅ Cache corrompido do Vite, .NET ou pnpm
- ✅ Preparar ambiente para CI/CD

## 🚀 Métodos de Execução

### Método 1: Script Automático (Recomendado)

```bash
# Com confirmação
pnpm run clean-branch

# Sem confirmação (força)
pnpm run clean-branch:force
```

### Método 2: Scripts Diretos

**Windows (PowerShell):**
```powershell
powershell -ExecutionPolicy Bypass -File scripts/clean-branch.ps1
```

**Linux/macOS (Bash):**
```bash
bash scripts/clean-branch.sh
```

### Método 3: Limpeza Manual

Se preferir fazer manualmente, execute os comandos abaixo:

```bash
# 1. Limpar frontend (desktop/)
rm -rf desktop/node_modules
rm -rf desktop/dist
rm -rf desktop/dist-electron
rm -rf desktop/release
rm -rf desktop/api-dist
rm -rf desktop/.vite
rm -rf desktop/.tsbuildinfo

# 2. Limpar backend (.NET)
rm -rf FirebirdApi/bin
rm -rf FirebirdApi/obj
rm -rf FirebirdTest/bin
rm -rf FirebirdTest/obj

# 3. Limpar dependências da raiz
rm -rf node_modules

# 4. Limpar caches
pnpm store prune
dotnet clean
dotnet nuget locals all --clear

# 5. Remover lock files (opcional)
rm -f desktop/pnpm-lock.yaml
rm -f pnpm-lock.yaml
```

## 📋 O Que É Removido

### Frontend (desktop/)
- `node_modules/` - Dependências do Node.js
- `dist/` - Build do Vite
- `dist-electron/` - Build do Electron
- `release/` - Builds finais do Electron Builder
- `api-dist/` - API distribuída
- `.vite/` - Cache do Vite
- `.tsbuildinfo` - Cache do TypeScript

### Backend (.NET)
- `FirebirdApi/bin/` - Binários compilados
- `FirebirdApi/obj/` - Arquivos temporários de build
- `FirebirdTest/bin/` - Binários de teste
- `FirebirdTest/obj/` - Objetos de teste

### Caches e Temporários
- Cache do pnpm (`pnpm store prune`)
- Cache do .NET (`dotnet nuget locals all --clear`)
- Lock files (opcional, para forçar reinstalação)

## 🔄 Processo de Reconstrução

Após a limpeza, reconstrua o projeto:

```bash
# 1. Instalar dependências
pnpm install

# 2. Build da API
cd FirebirdApi
dotnet build
cd ..

# 3. Build do frontend
cd desktop
pnpm build:full
```

## ⚠️ Avisos Importantes

1. **Backup**: Este processo remove TODOS os builds e caches. Certifique-se de que não há trabalho não salvo.

2. **Processos em Execução**: O script verifica se há processos do Electron, API ou Node.js em execução. Finalize-os antes da limpeza.

3. **Tempo**: A primeira reconstrução após limpeza pode demorar mais, pois todas as dependências serão baixadas novamente.

4. **Espaço em Disco**: A limpeza libera espaço significativo, mas a reinstalação pode usar mais banda e espaço temporariamente.

## 🐛 Solução de Problemas

### Erro de Permissão (Windows)
```powershell
# Execute como Administrador ou ajuste a política de execução
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Erro de Permissão (Linux/macOS)
```bash
# Torne o script executável
chmod +x scripts/clean-branch.sh
```

### Processo em Execução
```bash
# Finalize processos manualmente
pkill -f electron
pkill -f FirebirdApi
pkill -f node
```

## 📊 Benefícios

- ✅ **Builds Limpos**: Elimina problemas de cache corrompido
- ✅ **Dependências Atualizadas**: Força reinstalação com versões corretas
- ✅ **Ambiente Consistente**: Garante que todos tenham o mesmo estado
- ✅ **Debugging**: Facilita identificação de problemas reais vs. cache
- ✅ **CI/CD**: Prepara ambiente similar ao de produção

## 🔧 Personalização

Os scripts podem ser personalizados editando:
- `scripts/clean-branch.ps1` (Windows)
- `scripts/clean-branch.sh` (Linux/macOS)

Adicione ou remova diretórios conforme necessário para seu workflow específico.

---

**💡 Dica**: Execute `pnpm run clean-branch` sempre que mudar de branch para garantir um ambiente limpo e consistente!
