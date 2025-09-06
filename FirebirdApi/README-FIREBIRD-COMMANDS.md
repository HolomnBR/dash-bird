# Comandos Firebird - Sistema de Execução Remota

Este documento descreve o sistema de comandos Firebird implementado para execução remota via gRPC.

## Visão Geral

O sistema permite que o servidor cloud envie comandos SQL para serem executados nos nós desktop conectados, utilizando o `IFirebirdService` existente. A implementação é simples e direta, aproveitando a infraestrutura já disponível.

## Estrutura de Metadados

Todos os comandos Firebird utilizam uma estrutura de metadados estruturada que inclui:

- **`databaseId`** (string, opcional): ID da base de dados onde executar o comando. Se não especificado, usa a base padrão.
- **`sql`** (string, obrigatório para comandos SQL): Comando SQL a ser executado
- **`type`** (string, opcional): Tipo do comando (SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, DROP, EXECUTE, Other)
- **`tableName`** (string, obrigatório para comandos de tabela): Nome da tabela para comandos específicos
- **`timeout`** (int, opcional): Timeout em segundos. Padrão: 30
- **`validate`** (bool, opcional): Se deve validar a sintaxe do comando. Padrão: true
- **`additionalParameters`** (object, opcional): Parâmetros adicionais específicos do comando

A estrutura é convertida automaticamente entre JSON string (para transmissão gRPC) e objeto estruturado (para processamento interno).

## Comandos Disponíveis

### 1. FIREBIRD_EXECUTE_SQL
Executa um comando SQL genérico no Firebird.

**Metadados necessários (estrutura estruturada):**
```json
{
  "databaseId": "db-001",
  "sql": "SELECT * FROM USUARIOS WHERE ID = 1",
  "type": "SELECT",
  "timeout": 30,
  "validate": true
}
```

**Estrutura de metadados:**
- `databaseId` (string, opcional): ID da base de dados onde executar o comando. Se não especificado, usa a base padrão.
- `sql` (string, obrigatório): Comando SQL a ser executado
- `type` (string, opcional): Tipo do comando (SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, DROP, EXECUTE, Other). Padrão: SELECT
- `timeout` (int, opcional): Timeout em segundos. Padrão: 30
- `validate` (bool, opcional): Se deve validar a sintaxe do comando. Padrão: true

**Tipos de comando suportados:**
- `SELECT` - Consultas
- `INSERT` - Inserções
- `UPDATE` - Atualizações
- `DELETE` - Exclusões
- `CREATE` - Criação de objetos
- `ALTER` - Modificação de objetos
- `DROP` - Exclusão de objetos
- `EXECUTE` - Execução de procedures
- `Other` - Outros comandos

**Resposta:**
```json
{
  "success": true,
  "commandId": "guid",
  "rowsAffected": 1,
  "executionTime": 150.5,
  "executedAt": "2024-01-01T10:00:00Z",
  "error": null,
  "data": {...},
  "databaseId": "db-001"
}
```

### 2. FIREBIRD_QUERY
Executa especificamente uma query SELECT e retorna os dados.

**Metadados necessários:**
```json
{
  "databaseId": "db-001",
  "sql": "SELECT NOME, EMAIL FROM USUARIOS"
}
```

**Resposta:**
```json
{
  "success": true,
  "commandId": "guid",
  "rowCount": 2,
  "columnNames": ["NOME", "EMAIL"],
  "rows": [
    {"NOME": "João", "EMAIL": "joao@email.com"},
    {"NOME": "Maria", "EMAIL": "maria@email.com"}
  ],
  "executionTime": 120.3,
  "executedAt": "2024-01-01T10:00:00Z",
  "error": null,
  "databaseId": "db-001"
}
```

### 3. FIREBIRD_LIST_TABLES
Lista todas as tabelas de uma base de dados.

**Metadados necessários:**
```json
{
  "databaseId": "db-001"
}
```

**Resposta:**
```json
{
  "success": true,
  "commandId": "guid",
  "tables": [
    {
      "TABLE_NAME": "USUARIOS",
      "DESCRIPTION": "Tabela de usuários",
      "OBJECT_TYPE": "TABLE"
    },
    {
      "TABLE_NAME": "PRODUTOS",
      "DESCRIPTION": "Tabela de produtos",
      "OBJECT_TYPE": "VIEW"
    }
  ],
  "tableCount": 2,
  "executionTime": 45.2,
  "executedAt": "2024-01-01T10:00:00Z",
  "error": null,
  "databaseId": "db-001"
}
```

### 4. FIREBIRD_LIST_COLUMNS
Lista as colunas de uma tabela específica.

**Metadados necessários:**
```json
{
  "databaseId": "db-001",
  "tableName": "USUARIOS"
}
```

**Resposta:**
```json
{
  "success": true,
  "commandId": "guid",
  "tableName": "USUARIOS",
  "columns": [
    {
      "COLUMN_NAME": "ID",
      "FIELD_TYPE": 8,
      "FIELD_LENGTH": 4,
      "IS_NULLABLE": false,
      "DEFAULT_VALUE": null,
      "DESCRIPTION": "ID único do usuário"
    },
    {
      "COLUMN_NAME": "NOME",
      "FIELD_TYPE": 14,
      "FIELD_LENGTH": 100,
      "IS_NULLABLE": true,
      "DEFAULT_VALUE": null,
      "DESCRIPTION": "Nome do usuário"
    }
  ],
  "columnCount": 2,
  "executionTime": 30.1,
  "executedAt": "2024-01-01T10:00:00Z",
  "error": null,
  "databaseId": "db-001"
}
```

### 5. FIREBIRD_DATABASE_INFO
Obtém informações detalhadas sobre uma base de dados.

**Metadados necessários:**
```json
{
  "databaseId": "db-001"
}
```

**Resposta:**
```json
{
  "success": true,
  "commandId": "guid",
  "databaseInfo": {
    "databaseName": "MinhaBase",
    "serverVersion": "WI-V4.0.5.2704 Firebird 4.0",
    "databaseSize": 1048576,
    "tableCount": 15,
    "isConnected": true,
    "lastBackup": "2024-01-01T00:00:00Z",
    "connectionString": "Server=localhost;Port=3050;..."
  },
  "databaseId": "db-001",
  "timestamp": "2024-01-01T10:00:00Z"
}
```

### 6. FIREBIRD_TEST_CONNECTION
Testa a conexão com uma base de dados.

**Metadados necessários:**
```json
{
  "databaseId": "db-001"
}
```

**Resposta:**
```json
{
  "success": true,
  "commandId": "guid",
  "connectionStatus": {
    "isConnected": true,
    "serverVersion": "WI-V4.0.5.2704 Firebird 4.0",
    "responseTime": 25.5,
    "testedAt": "2024-01-01T10:00:00Z",
    "error": null
  },
  "databaseId": "db-001",
  "timestamp": "2024-01-01T10:00:00Z"
}
```

## Segurança

### Validações Implementadas

1. **Comandos Perigosos Bloqueados:**
   - `DROP DATABASE`
   - `SHUTDOWN`
   - `CREATE DATABASE`
   - `ALTER DATABASE`

2. **Validação de Sintaxe:**
   - Pode ser desabilitada via parâmetro `validate: false`
   - Verifica comandos perigosos por padrão

3. **Timeout de Execução:**
   - Padrão: 30 segundos
   - Configurável via parâmetro `timeout`

4. **Isolamento de Conexão:**
   - Cada comando usa sua própria conexão
   - Conexões são fechadas automaticamente
   - Não há persistência de estado entre comandos

## Tratamento de Erros

Todos os comandos retornam uma estrutura de resposta consistente:

```json
{
  "success": false,
  "error": "Mensagem de erro detalhada",
  "commandId": "guid",
  "timestamp": "2024-01-01T10:00:00Z"
}
```

### Tipos de Erro Comuns

1. **Erro de Conexão:**
   - Base de dados não encontrada
   - Credenciais inválidas
   - Servidor inacessível

2. **Erro de SQL:**
   - Sintaxe inválida
   - Tabela não encontrada
   - Violação de constraints

3. **Erro de Segurança:**
   - Comando perigoso detectado
   - Timeout de execução

## Exemplos de Uso

### Exemplo 1: Consulta Simples
```json
{
  "command_text": "FIREBIRD_QUERY",
  "command_id": "cmd-001",
  "metadata": "{\"databaseId\": \"db-001\", \"sql\": \"SELECT COUNT(*) FROM USUARIOS\"}"
}
```

### Exemplo 2: Inserção de Dados
```json
{
  "command_text": "FIREBIRD_EXECUTE_SQL",
  "command_id": "cmd-002",
  "metadata": "{\"databaseId\": \"db-001\", \"sql\": \"INSERT INTO USUARIOS (NOME, EMAIL) VALUES ('João', 'joao@email.com')\", \"type\": \"INSERT\"}"
}
```

### Exemplo 3: Listar Estrutura
```json
{
  "command_text": "FIREBIRD_LIST_TABLES",
  "command_id": "cmd-003",
  "metadata": "{\"databaseId\": \"db-001\"}"
}
```

### Exemplo 4: Comando com Parâmetros Adicionais
```json
{
  "command_text": "FIREBIRD_EXECUTE_SQL",
  "command_id": "cmd-004",
  "metadata": "{\"databaseId\": \"db-001\", \"sql\": \"SELECT * FROM USUARIOS WHERE ATIVO = 1\", \"type\": \"SELECT\", \"timeout\": 60, \"validate\": true}"
}
```

### Exemplo 5: Listar Colunas de Tabela
```json
{
  "command_text": "FIREBIRD_LIST_COLUMNS",
  "command_id": "cmd-005",
  "metadata": "{\"databaseId\": \"db-001\", \"tableName\": \"USUARIOS\"}"
}
```

## Logs e Monitoramento

O sistema gera logs detalhados para cada operação:

- **Início da execução:** Comando, ID, parâmetros
- **Resultado:** Sucesso/falha, tempo de execução, linhas afetadas
- **Erros:** Stack trace completo para debugging

## Configuração

Os comandos Firebird são integrados diretamente no `CommandStreamService` existente, utilizando as dependências já injetadas:

- `IFirebirdService` - Para execução de comandos SQL
- `IDatabaseConfigService` - Para gerenciamento de bases de dados
- `ILogger<CommandStreamService>` - Para logging

Não requer configuração adicional, aproveitando a infraestrutura existente.

## Limitações

1. **Tamanho de Resultado:** Não há limite específico, mas grandes resultados podem impactar performance
2. **Concorrência:** Comandos são executados sequencialmente por conexão
3. **Transações:** Cada comando é executado em sua própria transação
4. **Procedures:** Suporte básico para procedures via comando `EXECUTE`

## Troubleshooting

### Problema: Comando não é reconhecido
**Solução:** Verificar se o `command_text` está correto e em maiúsculas

### Problema: Erro de conexão
**Solução:** Verificar se a base de dados está configurada e acessível

### Problema: Timeout de execução
**Solução:** Aumentar o valor do parâmetro `timeout` ou otimizar a query

### Problema: Comando bloqueado por segurança
**Solução:** Verificar se o comando não contém operações perigosas ou desabilitar validação
