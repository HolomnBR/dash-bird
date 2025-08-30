# Firebird API

API para gerenciamento de bases de dados Firebird com sistema de configuração múltipla.

## Funcionalidades

### Sistema de Configuração de Bases de Dados
- ✅ **Listar bases adicionadas** - Visualizar todas as bases configuradas
- ✅ **Adicionar base** - Adicionar nova base com geração automática de GUID
- ✅ **Remover base** - Remover base da configuração (soft delete)
- ✅ **Testar conexão** - Verificar conectividade por ID da base
- ✅ **Listar tabelas** - Obter todas as tabelas de uma base específica
- ✅ **Gerenciar configurações** - Atualizar e visualizar configurações

### Operações SQL
- Teste de conexão com base padrão ou por ID
- Execução de queries SQL
- Execução de comandos não-query
- Execução de comandos escalar

## Como Executar

### Pré-requisitos
- .NET 6.0 ou superior
- Firebird Server instalado e rodando
- Acesso às bases de dados Firebird (.FDB)

### Executar a Aplicação
```bash
cd FirebirdApi
python -m uvicorn main:app --reload --port=4200
```

**Alternativa (se uvicorn não estiver disponível):**
```bash
cd FirebirdApi
dotnet run
```

Swagger/OpenAPI estará disponível em: `https://localhost:4200/swagger` (ou `http://localhost:5175/swagger` quando rodando com dotnet)

## Endpoints Principais

### Configuração de Bases de Dados
- `GET /api/DatabaseConfig/databases` - Listar todas as bases
- `POST /api/DatabaseConfig/databases` - Adicionar nova base
- `GET /api/DatabaseConfig/databases/{id}` - Obter base específica
- `PUT /api/DatabaseConfig/databases/{id}` - Atualizar base
- `DELETE /api/DatabaseConfig/databases/{id}` - Remover base
- `GET /api/DatabaseConfig/databases/{id}/test-connection` - Testar conexão
- `GET /api/DatabaseConfig/databases/{id}/tables` - Listar tabelas

### Operações Firebird (Legado)
- `GET /api/Firebird/test-connection` - Testar conexão padrão
- `POST /api/Firebird/execute-query` - Executar query
- `POST /api/Firebird/execute-non-query` - Executar comando
- `POST /api/Firebird/execute-scalar` - Executar escalar

## Configuração

### Arquivo de Configuração Padrão
O arquivo `appsettings.json` contém a configuração padrão da base TAVAGUA.

### Arquivo de Configurações Múltiplas
As bases adicionadas via API são salvas em `database-configs.json` no diretório de execução.

## Exemplo de Uso

### 1. Adicionar Base de Dados
```bash
curl -X POST "https://localhost:4200/api/DatabaseConfig/databases" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MinhaBase",
    "server": "localhost",
    "database": "C:\\caminho\\para\\base.FDB",
    "username": "SYSDBA",
    "password": "masterkey",
    "port": 3050,
    "charset": "UTF8"
  }'
```

### 2. Testar Conexão
```bash
curl -X GET "https://localhost:4200/api/DatabaseConfig/databases/{guid}/test-connection"
```

### 3. Listar Tabelas
```bash
curl -X GET "https://localhost:4200/api/DatabaseConfig/databases/{guid}/tables"
```

## Documentação Completa

Para documentação detalhada da API, consulte:
- `API_Documentation.md` - Documentação completa dos endpoints
- `test-api.http` - Arquivo de testes HTTP

## Estrutura do Projeto

```
FirebirdApi/
├── Controllers/
│   ├── FirebirdController.cs      # Endpoints de operações SQL
│   └── DatabaseConfigController.cs # Sistema de configuração de bases
├── Models/
│   ├── DatabaseConfig.cs          # Modelo de configuração
│   └── TableInfo.cs               # Informações de tabelas
├── Services/
│   ├── FirebirdService.cs         # Serviço legado
│   └── DatabaseConfigService.cs   # Serviço de configuração
├── appsettings.json               # Configuração padrão
├── API_Documentation.md           # Documentação da API
└── test-api.http                  # Testes HTTP
```

## Características Técnicas

- **Persistência**: Configurações salvas em arquivo JSON
- **Identificação**: GUID único para cada base de dados
- **Validação**: Verificação de campos obrigatórios
- **Segurança**: Soft delete para remoção de bases
- **Compatibilidade**: Mantém endpoints legados funcionando
- **Porta**: API roda na porta 4200 (configurável)

## Notas de Segurança

⚠️ **Atenção**: Em ambiente de produção, considere:
- Criptografar senhas armazenadas
- Usar HTTPS
- Implementar autenticação/autorização
- Validar caminhos de arquivos
- Limitar acesso aos endpoints sensíveis

## Eventos do Firebird (POST_EVENT) e Listener em C#

Os eventos nativos do Firebird permitem sinalizar mudanças no banco e notificar aplicações cliente. A forma usual é publicar um evento dentro de um trigger e, no C#, ouvir esse evento com `FbRemoteEvent` do pacote `FirebirdSql.Data.FirebirdClient`.

### 1) Disparando um evento no banco (trigger)

Exemplo: após inserir em `VENDAS`, publicar o evento `vendas_insert`.

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

Observações:
- O evento só é emitido após o COMMIT da transação.
- O evento não carrega payload (nenhuma linha ou ID). Trate-o como um “sinal”. Depois, consulte o banco (por PK, timestamp, fila, etc.).
- Eventos podem ser coalescidos/agrupados pelo servidor. Ouça como “algo mudou”.

### 2) Ouvindo o evento no C# (FbRemoteEvent)

```csharp
using FirebirdSql.Data.FirebirdClient;

// Exemplo de connection string:
// "Server=localhost;Port=3050;Database=C:\\db\\MYDB.FDB;User=SYSDBA;Password=masterkey;Charset=UTF8;Dialect=3;"
var connectionString = "sua-connection-string-aqui";

var remoteEvent = new FbRemoteEvent(connectionString);
remoteEvent.AddEvent("vendas_insert");

remoteEvent.EventReceived += (sender, args) =>
{
    var eventName = args.EventName ?? args.Name;
    if (string.Equals(eventName, "vendas_insert", StringComparison.OrdinalIgnoreCase))
    {
        // Dispare sua lógica aqui (ex.: buscar vendas mais recentes)
        // Dica: use limites de data/hora ou uma tabela de fila para identificar mudanças.
    }
};

// Inicia a escuta (mantém uma conexão interna ativa). Mantenha o objeto vivo (singleton/hosted service).
remoteEvent.QueueEvents();

// Para parar: remoteEvent.CancelEvents();
```

Boas práticas de integração:
- Mantenha o listener vivo (ex.: `IHostedService` singleton) e resiliente a reconexões.
- Use `ORDER BY` e janelas de tempo (ou uma “tabela de fila”) para buscar mudanças após o evento.
- Evite trabalho pesado direto no handler; delegue para um serviço ou fila em memória.

Compatibilidade no projeto:
- O pacote `FirebirdSql.Data.FirebirdClient` já está referenciado em `FirebirdApi.csproj`.
- Para integração futura, podemos criar um hosted service que emite notificações (ex.: via SignalR ou endpoint interno) quando eventos chegarem.