# Método de Snapshot da Base de Dados

## Visão Geral

O método `GenerateDatabaseSnapshotAsync` foi criado para gerar um snapshot completo de uma base de dados Firebird, incluindo todas as informações necessárias para análise e sincronização posterior.

## Funcionalidades

### 1. Listar Todas as Tabelas
- Recupera todas as tabelas não-sistema da base de dados
- Inclui nome, schema, tipo e descrição de cada tabela

### 2. Recuperar Schema de Cada Tabela
- Para cada tabela, obtém a estrutura completa das colunas
- Inclui tipo de dados, tamanho, precisão, escala, nullable, chave primária e valores padrão

### 3. Contar Quantidade de Registros
- Executa `SELECT COUNT(*)` para cada tabela
- Armazena o total de registros em cada tabela

### 4. Obter LastId
- Identifica automaticamente a chave primária da tabela
- Se não houver chave primária, procura por colunas que contenham "ID", "CODIGO" ou "COD"
- Executa `SELECT MAX(coluna)` para obter o último ID

### 5. Salvar em JSON Local
- Cria uma pasta `snapshots` no diretório da aplicação
- Gera arquivo com timestamp: `snapshot_{databaseId}_{yyyyMMdd_HHmmss}.json`
- Formato JSON indentado para fácil leitura

## Endpoint da API

```
POST /api/DatabaseConfig/databases/{id}/generate-snapshot
```

### Parâmetros
- `id`: ID da base de dados configurada

### Resposta
```json
{
  "success": true,
  "data": {
    "databaseId": "guid-da-base",
    "databaseName": "Nome da Base",
    "generatedAt": "2024-01-01T12:00:00Z",
    "tables": [
      {
        "tableName": "NOME_TABELA",
        "schema": "SCHEMA",
        "tableType": "Table",
        "description": "Descrição da tabela",
        "columns": [...],
        "recordCount": 1000,
        "lastId": 999,
        "generatedAt": "2024-01-01T12:00:00Z"
      }
    ]
  },
  "message": "Snapshot gerado com sucesso e salvo localmente. Total de tabelas processadas: X"
}
```

## Estrutura do Arquivo JSON

### DatabaseSnapshot
- `databaseId`: Identificador único da base
- `databaseName`: Nome amigável da base
- `generatedAt`: Timestamp de quando o snapshot foi gerado
- `tables`: Lista de todas as tabelas processadas

### TableFullInfo
- `tableName`: Nome da tabela
- `schema`: Schema da tabela
- `tableType`: Tipo (Table, View, etc.)
- `description`: Descrição da tabela
- `columns`: Lista de colunas com schema completo
- `recordCount`: Total de registros na tabela
- `lastId`: Último ID encontrado (chave primária ou coluna ID)
- `generatedAt`: Timestamp de quando a tabela foi processada

### ColumnSchema
- `columnName`: Nome da coluna
- `dataType`: Tipo de dados
- `length`: Tamanho (para CHAR/VARCHAR)
- `precision`: Precisão (para NUMERIC/DECIMAL)
- `scale`: Escala (para NUMERIC/DECIMAL)
- `isNullable`: Se permite valores nulos
- `isPrimaryKey`: Se é chave primária
- `default`: Valor padrão
- `description`: Descrição da coluna

## Uso

### 1. Via API
```bash
curl -X POST "http://localhost:5000/api/DatabaseConfig/databases/{id}/generate-snapshot"
```

### 2. Via Código
```csharp
var snapshot = await _configService.GenerateDatabaseSnapshotAsync(databaseId);
```

## Tratamento de Erros

- Se uma tabela específica falhar durante o processamento, o erro é logado mas o processo continua
- Tabelas com problemas são incluídas no resultado com dados parciais
- Erros de conexão ou base inexistente retornam exceção apropriada

## Arquivos Gerados

Os snapshots são salvos na pasta `snapshots` com o formato:
```
snapshots/
├── snapshot_default_20240101_120000.json
├── snapshot_guid1_20240101_130000.json
└── snapshot_guid2_20240101_140000.json
```

## Casos de Uso

1. **Auditoria**: Verificar estrutura e volume de dados em um momento específico
2. **Sincronização**: Preparar dados para envio a servidor remoto
3. **Backup de Metadados**: Preservar estrutura das tabelas
4. **Análise de Performance**: Identificar tabelas com muitos registros
5. **Migração**: Documentar estado atual antes de alterações

## Limitações

- Assume que colunas ID são numéricas
- Processamento sequencial (não paralelo) para evitar sobrecarga da base
- Depende de permissões adequadas na base de dados
- Arquivos JSON podem ser grandes para bases com muitas tabelas

## Futuras Implementações

- [ ] Processamento paralelo para melhor performance
- [ ] Compressão dos arquivos JSON
- [ ] Envio automático para servidor remoto
- [ ] Agendamento de snapshots periódicos
- [ ] Comparação entre snapshots para detectar mudanças
