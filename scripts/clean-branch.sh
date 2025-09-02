#!/bin/bash

# Script de limpeza completa para mudança de branch
# Remove todos os caches, builds e dependências para garantir build limpo

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

echo -e "${CYAN}🧹 Dash Bird - Script de Limpeza Completa${NC}"
echo -e "${CYAN}===============================================${NC}"

# Verificar se deve pular confirmação
if [[ "$1" != "--force" && "$1" != "-f" ]]; then
    echo -e "${YELLOW}⚠️  Isso irá remover TODOS os caches, builds e dependências.${NC}"
    read -p "Continuar? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${RED}❌ Operação cancelada pelo usuário.${NC}"
        exit 0
    fi
fi

echo -e "\n${YELLOW}🚀 Iniciando limpeza completa...${NC}"

# Função para remover diretório com verificação
remove_directory_safe() {
    local path="$1"
    local description="$2"
    
    if [ -d "$path" ]; then
        echo -e "${YELLOW}🗑️  Removendo $description...${NC}"
        if rm -rf "$path"; then
            echo -e "${GREEN}✅ $description removido com sucesso${NC}"
        else
            echo -e "${RED}❌ Erro ao remover $description${NC}"
        fi
    else
        echo -e "${GRAY}ℹ️  $description não encontrado, pulando...${NC}"
    fi
}

# Função para limpar cache do pnpm
clear_pnpm_cache() {
    echo -e "\n${YELLOW}📦 Limpando cache do pnpm...${NC}"
    if command -v pnpm &> /dev/null; then
        pnpm store prune || echo -e "${RED}❌ Erro ao limpar cache do pnpm${NC}"
        echo -e "${GREEN}✅ Cache do pnpm limpo${NC}"
    else
        echo -e "${GRAY}ℹ️  pnpm não encontrado, pulando...${NC}"
    fi
}

# Função para limpar cache do .NET
clear_dotnet_cache() {
    echo -e "\n${YELLOW}🔧 Limpando cache do .NET...${NC}"
    if command -v dotnet &> /dev/null; then
        dotnet clean --verbosity quiet || true
        dotnet nuget locals all --clear || true
        echo -e "${GREEN}✅ Cache do .NET limpo${NC}"
    else
        echo -e "${GRAY}ℹ️  dotnet não encontrado, pulando...${NC}"
    fi
}

# 1. Limpar diretórios de build do frontend (desktop/)
echo -e "\n${CYAN}🖥️  Limpando builds do frontend...${NC}"
remove_directory_safe "desktop/node_modules" "node_modules do desktop"
remove_directory_safe "desktop/dist" "dist do Vite"
remove_directory_safe "desktop/dist-electron" "dist-electron"
remove_directory_safe "desktop/release" "release builds"
remove_directory_safe "desktop/api-dist" "api-dist"

# 2. Limpar diretórios de build do backend (.NET)
echo -e "\n${CYAN}🔧 Limpando builds do backend...${NC}"
remove_directory_safe "FirebirdApi/bin" "bin do FirebirdApi"
remove_directory_safe "FirebirdApi/obj" "obj do FirebirdApi"
remove_directory_safe "FirebirdTest/bin" "bin do FirebirdTest"
remove_directory_safe "FirebirdTest/obj" "obj do FirebirdTest"

# 3. Limpar node_modules da raiz (se existir)
echo -e "\n${CYAN}📁 Limpando dependências da raiz...${NC}"
remove_directory_safe "node_modules" "node_modules da raiz"

# 4. Limpar caches específicos
echo -e "\n${CYAN}💾 Limpando caches...${NC}"

# Cache do Vite
remove_directory_safe "desktop/.vite" "cache do Vite"

# Cache do TypeScript
remove_directory_safe "desktop/.tsbuildinfo" "cache do TypeScript"

# Cache do Electron Builder
remove_directory_safe "desktop/app-builds" "cache do Electron Builder"

# Cache do pnpm
clear_pnpm_cache

# Cache do .NET
clear_dotnet_cache

# 5. Limpar arquivos temporários
echo -e "\n${CYAN}🗂️  Limpando arquivos temporários...${NC}"

# Arquivos de lock que podem estar desatualizados
lock_files=("desktop/pnpm-lock.yaml" "pnpm-lock.yaml")

for lock_file in "${lock_files[@]}"; do
    if [ -f "$lock_file" ]; then
        echo -e "${YELLOW}🗑️  Removendo $lock_file...${NC}"
        rm -f "$lock_file"
    fi
done

# 6. Verificar se há processos em execução
echo -e "\n${CYAN}🔍 Verificando processos em execução...${NC}"

processes=("electron" "FirebirdApi" "node")
for process in "${processes[@]}"; do
    if pgrep -x "$process" > /dev/null; then
        echo -e "${YELLOW}⚠️  Processo $process está em execução. Considere finalizá-lo.${NC}"
    fi
done

echo -e "\n${GREEN}🎉 Limpeza completa finalizada!${NC}"
echo -e "${GREEN}===============================================${NC}"

echo -e "\n${CYAN}📋 Próximos passos recomendados:${NC}"
echo -e "${NC}1. Instalar dependências: pnpm install${NC}"
echo -e "${NC}2. Build da API: cd FirebirdApi && dotnet build${NC}"
echo -e "${NC}3. Build do frontend: cd desktop && pnpm build${NC}"
echo -e "${NC}4. Ou usar o build completo: cd desktop && pnpm build:full${NC}"

echo -e "\n${YELLOW}💡 Dica: Use 'pnpm run clean-branch' para executar este script facilmente!${NC}"
