# Cliente gRPC - DashBird

Este documento explica como configurar e usar o cliente gRPC para se conectar ao servidor DashBird.

## Configuração

### 1. Configuração do Servidor gRPC

O cliente está configurado para se conectar ao servidor gRPC em `https://localhost:7001` por padrão. Esta configuração pode ser alterada no arquivo `appsettings.json`:

```json
{
  "GrpcServer": {
    "Url": "https://localhost:7001",
    "Timeout": 30,
    "MaxRetryAttempts": 3
  }
}
```

### 2. Certificados SSL

O cliente está configurado para aceitar certificados SSL auto-assinados em ambiente de desenvolvimento (localhost). Para produção, configure certificados válidos.

## Como Usar

### 1. Iniciar o Servidor DashBird

Certifique-se de que o servidor DashBird está rodando:
```
📡 Endpoint gRPC: https://localhost:7001
🌐 API REST: https://localhost:7001/api
📚 Swagger UI: https://localhost:7001/swagger
📊 Status: https://localhost:7001/status
```

### 2. Iniciar a API Cliente

Execute o comando na pasta `FirebirdApi`:
```bash
dotnet run
```

### 3. Testar a Conexão

Acesse o endpoint de teste:
```
GET https://localhost:5000/health/grpc
```

Ou use o script PowerShell:
```powershell
.\test-grpc-connection.ps1
```

## Endpoints Disponíveis

### Health Check
- `GET /health` - Status geral da API
- `GET /health/grpc` - Teste de conexão gRPC

### Operações gRPC

O `GrpcClientService` oferece os seguintes métodos:

- `TestConnectionAsync(databaseId?)` - Testa conexão com o banco
- `ExecuteQueryAsync(query, databaseId?)` - Executa query SELECT
- `ExecuteNonQueryAsync(query, databaseId?)` - Executa comandos DML/DDL
- `ExecuteScalarAsync(query, databaseId?)` - Executa query escalar
- `GetTablesAsync(databaseId?)` - Lista tabelas do banco
- `GetTableInfoAsync(tableName, databaseId?)` - Informações de uma tabela

## Exemplo de Uso

```csharp
// Injetar o serviço no controller
public class MyController : ControllerBase
{
    private readonly IGrpcClientService _grpcClient;

    public MyController(IGrpcClientService grpcClient)
    {
        _grpcClient = grpcClient;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var isConnected = await _grpcClient.TestConnectionAsync();
            return Ok(new { connected = isConnected });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        try
        {
            var tables = await _grpcClient.GetTablesAsync();
            return Ok(tables);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

## Troubleshooting

### Erro de Certificado SSL
Se você receber erros de certificado SSL, verifique se:
1. O servidor DashBird está rodando em HTTPS
2. O certificado está configurado corretamente
3. Para desenvolvimento, o cliente está configurado para aceitar certificados auto-assinados

### Erro de Conexão
Se a conexão falhar:
1. Verifique se o servidor DashBird está rodando
2. Confirme a URL no `appsettings.json`
3. Verifique se não há firewall bloqueando a porta 7001
4. Teste o endpoint `/health/grpc` para diagnóstico

### Timeout
Se houver timeout:
1. Aumente o valor de `Timeout` no `appsettings.json`
2. Verifique a latência da rede
3. Confirme se o servidor está processando as requisições

## Logs

O cliente gRPC registra logs detalhados. Para ver os logs:
1. Configure o nível de log no `appsettings.json`
2. Monitore a saída do console durante a execução
3. Use o endpoint `/health/grpc` para verificar o status da conexão
