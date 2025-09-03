# Configuração do Servidor gRPC no DashBirdServer

Este documento explica como configurar o servidor gRPC no projeto DashBirdServer (servidor cloud) para receber as chamadas gRPC da FirebirdAPI.

## 1. Configuração do Projeto

### Adicionar Pacotes NuGet

```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.57.0" />
<PackageReference Include="Grpc.Tools" Version="2.59.0" PrivateAssets="All" />
```

### Configurar o arquivo .csproj

```xml
<ItemGroup>
  <Protobuf Include="Protos\*.proto" GrpcServices="Server" />
</ItemGroup>
```

## 2. Configuração no Program.cs

```csharp
using Grpc.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
});

// Registrar o serviço de registro de nós
builder.Services.AddScoped<NodeRegistrationGrpcService>();

var app = builder.Build();

// Configurar pipeline
app.UseRouting();

// Mapear serviços gRPC
app.MapGrpcService<NodeRegistrationGrpcService>();

// Configurar TLS para gRPC
app.UseHttpsRedirection();

app.Run();
```

## 3. Configuração de TLS

### Para Desenvolvimento (certificados auto-assinados)

```csharp
// No Program.cs
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 7001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps(httpsOptions =>
        {
            // Configurar certificado SSL para desenvolvimento
            httpsOptions.ServerCertificate = new X509Certificate2("path/to/cert.pfx", "password");
        });
    });
});
```

### Para Produção

```csharp
// Usar certificados válidos
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 443, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps();
    });
});
```

## 4. Implementação do Serviço

Copie o arquivo `DashBirdServerGrpcService.cs.example` para o projeto DashBirdServer e implemente as interfaces necessárias:

- `INodeRepository`: Para persistência de dados dos nós
- `IAuthService`: Para autenticação e geração de tokens

### Estrutura do Nó

O servidor deve implementar um modelo `DesktopNode` com a seguinte estrutura:

```csharp
public class DesktopNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? DatabasePath { get; set; }
    public string? Version { get; set; }
    public string? OperatingSystem { get; set; }
    public string? UserId { get; set; } // Se null = anônimo, se preenchido = autenticado
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsActive { get; set; }
    
    // Propriedade calculada
    public bool IsAnonymous => string.IsNullOrEmpty(UserId);
}
```

### Métodos do Serviço

1. **RegisterNode**: Registra um nó (funciona para autenticado e anônimo)
2. **LinkNodeToUser**: Vincula um nó anônimo a um usuário autenticado
3. **Outros métodos de sincronização**: StartInitialSync, SendSyncBatch, etc.

## 5. Configuração de CORS (se necessário)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGrpc", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// No pipeline
app.UseCors("AllowGrpc");
```

## 6. Configuração de Logging

```csharp
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configurar log level para gRPC
builder.Logging.AddFilter("Grpc", LogLevel.Information);
```

## 7. Configuração de Timeout

```csharp
builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 4 * 1024 * 1024;
    options.MaxSendMessageSize = 4 * 1024 * 1024;
    options.KeepAliveTime = TimeSpan.FromSeconds(30);
    options.KeepAliveTimeout = TimeSpan.FromSeconds(5);
    options.KeepAlivePermitWithoutCalls = true;
});
```

## 8. Configuração de Autenticação

```csharp
// Adicionar autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "DashBirdServer",
            ValidAudience = "DashBirdClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key"))
        };
    });

// Configurar autorização para gRPC
builder.Services.AddAuthorization();
```

## 9. Configuração de Monitoramento

```csharp
// Adicionar métricas
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcHealthChecks();

// No pipeline
app.MapGrpcReflectionService();
app.MapGrpcHealthChecksService();
```

## 10. Teste da Configuração

### Usar grpcurl para testar

```bash
# Testar conexão
grpcurl -plaintext localhost:7001 list

# Testar serviço específico
grpcurl -plaintext localhost:7001 dashbird.NodeRegistrationService/RegisterNode
```

### Usar Postman ou Insomnia

Configure uma requisição gRPC para:
- URL: `https://localhost:7001`
- Service: `dashbird.NodeRegistrationService`
- Method: `RegisterNode`

## 11. Configuração de Produção

### Variáveis de Ambiente

```bash
# .env
GRPC_PORT=443
GRPC_CERT_PATH=/path/to/cert.pfx
GRPC_CERT_PASSWORD=your-password
GRPC_MAX_MESSAGE_SIZE=4194304
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
EXPOSE 443
ENTRYPOINT ["dotnet", "DashBirdServer.dll"]
```

## 12. Monitoramento e Logs

### Configurar Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Configurar Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<GrpcHealthCheck>("grpc");

app.MapHealthChecks("/health");
```

## Próximos Passos

1. Implementar as interfaces `INodeRepository` e `IAuthService`
2. Configurar banco de dados para persistência
3. Implementar autenticação JWT
4. Configurar certificados SSL para produção
5. Testar a comunicação entre FirebirdAPI e DashBirdServer
6. Configurar monitoramento e logs
7. Implementar health checks
8. Configurar CI/CD para deploy

## Troubleshooting

### Erro de Certificado SSL

```bash
# Para desenvolvimento, desabilitar verificação SSL
export GRPC_SSL_TARGET_NAME_OVERRIDE=localhost
```

### Erro de Timeout

```csharp
// Aumentar timeout no cliente
var channelOptions = new GrpcChannelOptions
{
    HttpClient = httpClient,
    DisposeHttpClient = true,
    MaxReceiveMessageSize = 4 * 1024 * 1024,
    MaxSendMessageSize = 4 * 1024 * 1024
};
```

### Erro de CORS

```csharp
// Configurar CORS para gRPC
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGrpc", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```
