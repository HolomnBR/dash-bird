# Exemplos de Uso da API Firebird

## Adicionar Base de Dados

### Endpoint
```
POST /api/DatabaseConfig/databases
```

### Exemplo de Request (JSON correto)
```bash
curl -X 'POST' \
  'http://localhost:5175/api/DatabaseConfig/databases' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "name": "db01",
  "database": "C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB"
}'
```

### Campos Obrigatórios
- `database`: Caminho completo para o arquivo .FDB (obrigatório)

### Campos Opcionais (preenchidos automaticamente se não fornecidos)
- `name`: Nome da base de dados (padrão: nome do arquivo sem extensão)
- `server`: Servidor (padrão: "localhost")
- `username`: Usuário (padrão: "SYSDBA")
- `password`: Senha (padrão: "masterkey")
- `port`: Porta (padrão: 3050)
- `charset`: Charset (padrão: "UTF8")

### Exemplos de Request

#### Mínimo (apenas caminho do arquivo)
```json
{
  "database": "C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB"
}
```

#### Completo
```json
{
  "name": "TAVAGUA",
  "database": "C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB",
  "server": "localhost",
  "username": "SYSDBA",
  "password": "masterkey",
  "port": 3050,
  "charset": "UTF8"
}
```

### Resposta de Sucesso
```json
{
  "success": true,
  "data": {
    "id": "generated-id",
    "name": "TAVAGUA",
    "server": "localhost",
    "database": "C:\\Projetos\\estudos\\forti\\dash\\FirebirdApi\\dbs\\TAVAGUA.FDB",
    "username": "SYSDBA",
    "password": "masterkey",
    "port": 3050,
    "charset": "UTF8",
    "createdAt": "2024-01-01T00:00:00.000Z",
    "isActive": true
  },
  "message": "Base de dados adicionada com sucesso"
}
```

## Outros Endpoints

### Listar Bases de Dados
```bash
GET /api/DatabaseConfig/databases
```

### Testar Conexão
```bash
GET /api/DatabaseConfig/databases/{id}/test-connection
```

### Listar Tabelas
```bash
GET /api/DatabaseConfig/databases/{id}/tables
```

### Remover Base de Dados
```bash
DELETE /api/DatabaseConfig/databases/{id}
```

## Notas Importantes

1. **Escape de caracteres no JSON**: Use `\\` para barras invertidas em caminhos de arquivo
2. **Porta padrão**: A API roda na porta 5175 (segundo `launchSettings.json`). Swagger em `/swagger`.
3. **Valores padrão**: A API preenche automaticamente campos não fornecidos com valores padrão do Firebird
4. **Validação**: Apenas o campo `database` é obrigatório

## Eventos do Firebird (POST_EVENT) – Exemplos Rápidos

### SQL: Trigger que publica evento após INSERT
```sql
SET TERM ^ ;
CREATE TRIGGER T_VENDAS_AI FOR VENDAS
ACTIVE AFTER INSERT POSITION 0
AS
BEGIN
  POST_EVENT 'vendas_insert';
END^
SET TERM ; ^
```

### C#: Ouvindo com FbRemoteEvent
```csharp
using FirebirdSql.Data.FirebirdClient;

var cs = "Server=localhost;Port=3050;Database=C:\\db\\MYDB.FDB;User=SYSDBA;Password=masterkey;Charset=UTF8;Dialect=3;";
var re = new FbRemoteEvent(cs);
re.AddEvent("vendas_insert");
re.EventReceived += (s, e) =>
{
    var name = e.EventName ?? e.Name;
    if (string.Equals(name, "vendas_insert", StringComparison.OrdinalIgnoreCase))
    {
        // Buscar mudanças recentes aqui (ex.: por DATA/HORA ou fila)
    }
};
re.QueueEvents();
```

Notas:
- O evento é entregue após COMMIT e não traz payload. Trate como um sinal.
- Mantenha o listener vivo (singleton/hosted service) e trate reconexões.
