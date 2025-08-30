# API de Configuração de Bases de Dados Firebird

Esta API permite gerenciar múltiplas configurações de bases de dados Firebird com funcionalidades para adicionar, remover, testar conexões e listar tabelas.

## Sistema de Configuração

### Arquivos de Configuração
- **`config.json`** - Configuração base do projeto (não editável via API)
- **`database-configs.json`** - Configurações dinâmicas adicionadas via API

### Hierarquia de Configurações
1. **Configuração Base** (`config.json`) - Contém bases padrão e configurações do projeto
2. **Configurações Dinâmicas** (`database-configs.json`) - Bases adicionadas/removidas via API
3. **Base Padrão** - Definida em `config.json` e pode ser alterada via API

## Endpoints Disponíveis

### 1. Listar Bases de Dados Configuradas
```
GET /api/DatabaseConfig/databases
```
**Resposta:**
```json
{
  "success": true,
  "data": [
    {
      "id": "default",
      "name": "TAVAGUA",
      "server": "localhost",
      "database": "C:\\caminho\\para\\base.FDB",
      "username": "SYSDBA",
      "password": "masterkey",
      "port": 3050,
      "charset": "UTF8",
      "createdAt": "2024-01-01T00:00:00Z",
      "isActive": true
    }
  ]
}
```

### 2. Adicionar Nova Base de Dados
```
POST /api/DatabaseConfig/databases
```
**Body:**
```json
{
  "name": "Nome da Base (opcional)",
  "server": "localhost",
  "database": "C:\\caminho\\para\\base.FDB",
  "username": "SYSDBA",
  "password": "masterkey",
  "port": 3050,
  "charset": "UTF8"
}
```
**Resposta:**
```json
{
  "success": true,
  "data": {
    "id": "guid-gerado-automaticamente",
    "name": "Nome da Base",
    "server": "localhost",
    "database": "C:\\caminho\\para\\base.FDB",
    "username": "SYSDBA",
    "password": "masterkey",
    "port": 3050,
    "charset": "UTF8",
    "createdAt": "2024-01-01T00:00:00Z",
    "isActive": true
  },
  "message": "Base de dados adicionada com sucesso"
}
```

### 3. Remover Base de Dados (apenas da configuração)
```
DELETE /api/DatabaseConfig/databases/{id}
```
**Nota:** Não é possível remover a base padrão (ID: "default")
**Resposta:**
```json
{
  "success": true,
  "message": "Base de dados removida com sucesso"
}
```

### 4. Testar Conexão com Base de Dados
```
GET /api/DatabaseConfig/databases/{id}/test-connection
```
**Resposta:**
```json
{
  "success": true,
  "data": {
    "success": true,
    "message": "Conexão bem-sucedida!",
    "databaseId": "guid-da-base",
    "testedAt": "2024-01-01T00:00:00Z"
  }
}
```

### 5. Listar Tabelas de uma Base de Dados
```
GET /api/DatabaseConfig/databases/{id}/tables
```
**Resposta:**
```json
{
  "success": true,
  "data": [
    {
      "tableName": "NOME_TABELA",
      "schema": "OWNER",
      "tableType": "Table",
      "description": "Descrição da tabela"
    }
  ],
  "database": "Nome da Base"
}
```

### 6. Obter Configuração de Base Específica
```
GET /api/DatabaseConfig/databases/{id}
```
**Resposta:**
```json
{
  "success": true,
  "data": {
    "id": "guid-da-base",
    "name": "Nome da Base",
    "server": "localhost",
    "database": "C:\\caminho\\para\\base.FDB",
    "username": "SYSDBA",
    "password": "masterkey",
    "port": 3050,
    "charset": "UTF8",
    "createdAt": "2024-01-01T00:00:00Z",
    "isActive": true
  }
}
```

### 7. Atualizar Configuração de Base de Dados
```
PUT /api/DatabaseConfig/databases/{id}
```
**Body:** Mesmo formato do POST
**Resposta:**
```json
{
  "success": true,
  "data": {
    // Configuração atualizada
  },
  "message": "Base de dados atualizada com sucesso"
}
```

### 8. Obter Base de Dados Padrão
```
GET /api/DatabaseConfig/default-database
```
**Resposta:**
```json
{
  "success": true,
  "data": {
    // Configuração da base padrão
  }
}
```

### 9. Definir Base de Dados Padrão
```
POST /api/DatabaseConfig/default-database
```
**Body:**
```json
{
  "databaseId": "guid-da-base"
}
```
**Resposta:**
```json
{
  "success": true,
  "data": {
    // Configuração da nova base padrão
  },
  "message": "Base de dados definida como padrão com sucesso"
}
```

### 10. Obter Configurações do Projeto
```
GET /api/DatabaseConfig/project-config
```
**Resposta:**
```json
{
  "success": true,
  "data": {
    "databases": [
      // Lista de bases da configuração base
    ],
    "settings": {
      "defaultDatabaseId": "default",
      "autoLoadFromConfig": true,
      "configFilePath": "database-configs.json"
    }
  }
}
```

## Endpoints Legados (FirebirdController)

### Testar Conexão (com base padrão ou específica)
```
GET /api/Firebird/test-connection?databaseId={id}
```
**Parâmetros:**
- `databaseId` (opcional): ID da base específica. Se não informado, usa a base padrão.

### Executar Query (com base padrão ou específica)
```
POST /api/Firebird/execute-query?databaseId={id}
```
**Body:**
```json
{
  "query": "SELECT * FROM SUA_TABELA"
}
```

### Executar Comando (com base padrão ou específica)
```
POST /api/Firebird/execute-non-query?databaseId={id}
```
**Body:**
```json
{
  "query": "INSERT INTO SUA_TABELA (COLUNA) VALUES ('VALOR')"
}
```

### Executar Scalar (com base padrão ou específica)
```
POST /api/Firebird/execute-scalar?databaseId={id}
```
**Body:**
```json
{
  "query": "SELECT COUNT(*) FROM SUA_TABELA"
}
```

## Funcionalidades Principais

### ✅ **Sistema de Configuração Híbrido**
- **Configuração Base** (`config.json`) - Bases padrão do projeto
- **Configurações Dinâmicas** - Gerenciadas via API
- **Base Padrão** - Configurável via API

### ✅ **Geração Automática de GUID**
- Cada base de dados recebe um ID único automaticamente
- Base padrão mantém ID fixo "default"
- Não é necessário fornecer o ID ao adicionar uma nova base

### ✅ **Persistência Inteligente**
- Configurações base são preservadas
- Configurações dinâmicas são salvas em arquivo separado
- Mesclagem automática de configurações

### ✅ **Validações e Segurança**
- Verificação de campos obrigatórios
- Proteção da base padrão contra remoção
- Teste de conectividade antes de operações
- Tratamento de erros com mensagens claras

## Exemplos de Uso

### Adicionar Base de Dados TAVAGUA
```bash
curl -X POST "https://localhost:4200/api/DatabaseConfig/databases" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TAVAGUA",
    "server": "localhost",
    "database": "C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB",
    "username": "SYSDBA",
    "password": "masterkey",
    "port": 3050,
    "charset": "UTF8"
  }'
```

### Testar Conexão com Base Específica
```bash
curl -X GET "https://localhost:4200/api/DatabaseConfig/databases/{guid}/test-connection"
```

### Executar Query em Base Específica
```bash
curl -X POST "https://localhost:4200/api/Firebird/execute-query?databaseId={guid}" \
  -H "Content-Type: application/json" \
  -d '{"query": "SELECT * FROM CLIENTES"}'
```

### Definir Nova Base Padrão
```bash
curl -X POST "https://localhost:4200/api/DatabaseConfig/default-database" \
  -H "Content-Type: application/json" \
  -d '{"databaseId": "{guid}"}'
```

## Estrutura dos Arquivos de Configuração

### config.json (Configuração Base)
```json
{
  "databases": [
    {
      "id": "default",
      "name": "TAVAGUA",
      "server": "localhost",
      "database": "C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB",
      "username": "SYSDBA",
      "password": "masterkey",
      "port": 3050,
      "charset": "UTF8",
      "createdAt": "2024-01-01T00:00:00Z",
      "isActive": true
    }
  ],
  "settings": {
    "defaultDatabaseId": "default",
    "autoLoadFromConfig": true,
    "configFilePath": "database-configs.json"
  }
}
```

### database-configs.json (Configurações Dinâmicas)
```json
[
  {
    "id": "guid-gerado",
    "name": "NovaBase",
    "server": "localhost",
    "database": "C:\\caminho\\para\\nova-base.FDB",
    "username": "SYSDBA",
    "password": "masterkey",
    "port": 3050,
    "charset": "UTF8",
    "createdAt": "2024-01-01T00:00:00Z",
    "isActive": true
  }
]
```

## Notas Importantes

1. **Porta da API**: A aplicação roda na porta 4200
2. **Configuração Base**: O arquivo `config.json` contém bases padrão e não é editado via API
3. **Configurações Dinâmicas**: Salvas em `database-configs.json` e gerenciadas via API
4. **Base Padrão**: Pode ser alterada via API, mas a base "default" não pode ser removida
5. **Persistência**: Configurações são salvas automaticamente após cada operação
6. **Conectividade**: Sempre teste a conexão antes de usar uma base
7. **Segurança**: Em produção, considere criptografar senhas e usar HTTPS
8. **Backup**: Ambos os arquivos de configuração podem ser copiados para backup

## Fluxo de Funcionamento

1. **Inicialização**: Carrega `config.json` (bases padrão) + `database-configs.json` (bases dinâmicas)
2. **Operações**: Todas as operações são feitas na lista mesclada
3. **Persistência**: Apenas configurações dinâmicas são salvas em `database-configs.json`
4. **Base Padrão**: Sempre disponível via `config.json` ou definida via API
5. **Compatibilidade**: Endpoints legados funcionam com base padrão ou específica
