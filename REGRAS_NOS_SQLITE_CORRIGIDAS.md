# Regras para Gerenciamento de Nós no SQLite - CORRIGIDAS

## Problemas Identificados e Corrigidos

### 1. **Falta de MachineId na Criação de Nós**
**Problema**: O método `CreateLocalNodeWithSystemInfoAsync` não estava definindo o `MachineId`, que é obrigatório para garantir unicidade.

**Solução**: 
- Adicionado parâmetro `machineId` opcional
- Se não fornecido, gera automaticamente baseado no nome da máquina
- Sempre define o `MachineId` ao criar novos nós

### 2. **Inconsistência na Busca de Nós**
**Problema**: A busca por nome da máquina não considerava a regra de unicidade por `MachineId`.

**Solução**:
- Melhorado o método `GetLocalNodeByMachineNameAsync` com logs detalhados
- Prioriza busca por `MachineId` quando disponível
- Adiciona logs para rastreamento de problemas

### 3. **Falta de Garantia de Existência do Nó**
**Problema**: Não havia um método para garantir que sempre existe um nó local para a máquina atual.

**Solução**:
- Criado método `EnsureLocalNodeExistsAsync` que:
  - Busca primeiro por `MachineId` se fornecido
  - Busca por nome da máquina
  - Cria novo nó se não encontrar
- Adicionado endpoint `POST /api/LocalNode/ensure-exists`

## Regras de Unicidade Implementadas

### 1. **MachineId como Chave Única**
- Cada nó deve ter um `MachineId` único
- Busca por `MachineId` tem prioridade sobre busca por nome
- Se `MachineId` não for fornecido, é gerado automaticamente

### 2. **Prevenção de Duplicatas**
- Verifica existência por `MachineId` antes de criar novo nó
- Remove nós duplicados com mesmo `MachineName` mas `MachineId` diferente
- Atualiza nó existente em vez de criar duplicata

### 3. **Geração Automática de MachineId**
- Usa hash SHA256 do nome da máquina + `Environment.MachineName`
- Primeiros 16 caracteres do hash em lowercase
- Garante unicidade mesmo sem `MachineId` explícito

## Endpoints Disponíveis

### 1. **POST /api/LocalNode/save-local-node**
- Salva ou atualiza nó local com informações do sistema
- **OBRIGATÓRIO**: `MachineId` para garantir unicidade
- Atualiza nó existente se `MachineId` já existe

### 2. **GET /api/LocalNode/by-machine/{machineName}**
- Busca nó por nome da máquina
- Retorna 404 se não encontrado

### 3. **GET /api/LocalNode/by-machine-id/{machineId}**
- Busca nó por `MachineId` (chave única)
- Método preferido para busca

### 4. **POST /api/LocalNode/ensure-exists** ⭐ **NOVO**
- Garante que existe um nó local para a máquina
- Cria automaticamente se não existir
- **SOLUÇÃO** para o erro "Nó local não encontrado"

## Como Usar

### Para Resolver o Erro "Nó local não encontrado":

```http
POST /api/LocalNode/ensure-exists
Content-Type: application/json

{
  "machineName": "DESKTOP-78ROEFC",
  "machineId": "58bc0c2d-5fd8-4d25-80ec-f41cef3cb615"
}
```

### Para Salvar/Atualizar Nó com MachineId:

```http
POST /api/LocalNode/save-local-node
Content-Type: application/json

{
  "machineName": "DESKTOP-78ROEFC",
  "machineId": "58bc0c2d-5fd8-4d25-80ec-f41cef3cb615",
  "operatingSystem": "Windows",
  "systemVersion": "10.0.19044",
  "architecture": "x64"
}
```

## Logs de Debug

Os métodos agora incluem logs detalhados para facilitar o debug:

- `Nó encontrado por MachineName: {MachineName} (MachineId: {MachineId})`
- `Nó não encontrado por MachineName: {MachineName}`
- `Nó local já existe para MachineId: {MachineId}`
- `Nó local não encontrado, criando novo para: {MachineName}`

## Migração de Dados Existentes

Se você tem nós existentes sem `MachineId`:

1. Use o endpoint `ensure-exists` para cada máquina
2. O sistema gerará automaticamente um `MachineId` baseado no nome
3. Nós futuros devem sempre fornecer `MachineId` explícito

## Validações Implementadas

- ✅ `MachineId` obrigatório em `save-local-node`
- ✅ `MachineName` obrigatório em `ensure-exists`
- ✅ Verificação de duplicatas por `MachineId`
- ✅ Geração automática de `MachineId` quando necessário
- ✅ Logs detalhados para debug
- ✅ Tratamento de exceções robusto
